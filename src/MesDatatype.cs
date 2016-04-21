using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DragonQuestLoc
{
    class MesDatatype
    {
        struct MesCount
        {
            public uint Count;
        }

        struct MesKeyDeclaration
        {
            public uint ActiveCount;
            public uint Offset;
        }

        struct MesText
        {
            public uint Start;
            public uint End;

            public string LocalStringCopy;
        }

        struct MesKeyValue
        {
            public uint Offset;
            public MesText Key;
            public MesText Value;

            public bool Dirty;
        }
        
        struct MesFormat
        {
            public MesCount header;
            public MesKeyDeclaration[] key_listing;
            public MesKeyValue[] keys;
        }

        MesFormat data;

        public MesDatatype()
        {
            data = new MesFormat();
            data.header = new MesCount();
        }

        uint CountActive()
        {
            uint num_active = 0;

            foreach (MesKeyDeclaration kd in data.key_listing)
            {
                num_active += kd.ActiveCount;
            }

            return num_active;
        }

        public void ReadHeader(BinaryReader br)
        {
            data.header.Count = br.ReadUInt32();

            var keydec_list = new List<MesKeyDeclaration>();

            for (uint i = 0; i < data.header.Count; ++i)
            {
                MesKeyDeclaration temp;

                temp.ActiveCount = br.ReadUInt32();
                temp.Offset = br.ReadUInt32();

                keydec_list.Add(temp);
            }

            data.key_listing = keydec_list.ToArray();
        }

        public void Write(BinaryWriter bw)
        {
            // as we don't support adding/removing
            bw.Write(data.header.Count);

            // key will not have changed either
            foreach( MesKeyDeclaration keydec in data.key_listing )
            {
                bw.Write(keydec.ActiveCount);
                bw.Write(keydec.Offset);
            }

            long key_start = bw.BaseStream.Position;
            
            // now pad the known length

            uint keys_len = 0;

            foreach (MesKeyValue kv in data.keys)
            {
                keys_len += 4; // sizeof(kv.Offset)

                uint length = (kv.Key.End - kv.Key.Start);
                keys_len += length +1; // length of unaligned key name (to 4 bytes)
                keys_len += MesAlign(length + 1);
            }

            if (keys_len > 0)
            {
                bw.Write(new byte[keys_len]);
            }

            // now write the data (padded)

            for(int i = 0; i < data.keys.Length; ++ i)
            {
                MesKeyValue kv = data.keys[i]; // random access

                uint data_start = (uint)bw.BaseStream.Position;

                byte[] str_buf = Encoding.Unicode.GetBytes(kv.Value.LocalStringCopy);

                bw.Write(str_buf);
                bw.Write(new byte[2]); // eol

                uint align_excess = MesAlign((uint)str_buf.Length + 2);
                if (align_excess != 0)
                {
                    bw.Write(new byte[align_excess]);
                }
                
                // set back the raw data pointer
                kv.Offset = data_start;
                kv.Dirty = false;

                // copy back to array
                data.keys[i] = kv;
            }

            // now populate the blank key data
            bw.BaseStream.Position = key_start;

            foreach (MesKeyValue kv in data.keys)
            {
                bw.Write(kv.Offset);

                byte[] str_buf = Encoding.ASCII.GetBytes(kv.Key.LocalStringCopy);

                bw.Write(str_buf);
                bw.Write(new byte[1]); // eol

                uint align_excess = MesAlign((uint)str_buf.Length + 1);
                if( align_excess != 0 )
                {
                    bw.Write(new byte[align_excess]);
                }
            }
        }

        private uint MesAlign(uint val)
        {
            uint mod_val = (val % 4);

            if (mod_val != 0 )
            {
                return 4 - mod_val;
            }

            return 0;
        }

        void ReadMesText(BinaryReader br, ref MesText mt, Encoding enc)
        {
            long old_pos = br.BaseStream.Position;

            br.BaseStream.Position = (long)mt.Start;

            int name_len = (int)(mt.End - mt.Start);
            mt.LocalStringCopy = enc.GetString(br.ReadBytes(name_len));

            br.BaseStream.Position = old_pos;
        }

        public void ReadKeys(BinaryReader br)
        {
            uint active = CountActive();

            var key_list = new List<MesKeyValue>();

            for (uint i = 0; i < active; ++i)
            {
                var temp = new MesKeyValue();
                temp.Key = new MesText();

                temp.Dirty = false;
                temp.Offset = br.ReadUInt32();
                temp.Key.Start = (uint)br.BaseStream.Position;

                bool text_align = true;
                while (text_align)
                {
                    uint pos = (uint)br.BaseStream.Position;
                    byte[] block = br.ReadBytes(4);

                    for (uint j = 0; j < 4; ++j)
                    {
                        if (block[j] == 0)
                        {
                            temp.Key.End = pos + j;
                            text_align = false;
                            break;
                        }
                    }
                }

                ReadMesText(br, ref temp.Key, Encoding.ASCII);

                key_list.Add(temp);
            }

            data.keys = key_list.ToArray();
        }

        public void ReadValues(BinaryReader br)
        {
            for(int i = 0 ; i < data.keys.Length; ++i )
            {
                var temp = new MesText();

                temp.Start = data.keys[i].Offset;

                // seek to file
                br.BaseStream.Position = (long)data.keys[i].Offset;

                bool text_align = true;
                while (text_align)
                {
                    uint pos = (uint)br.BaseStream.Position;

                    ushort block_1 = br.ReadUInt16();
                    ushort block_2 = br.ReadUInt16();

                    if (block_1 == 0)
                    {
                        temp.End = pos;
                        text_align = false;
                        break;
                    }

                    if( block_2 == 0 )
                    {
                        temp.End = pos + 2;
                        text_align = false;
                        break;
                    }

                    if( br.BaseStream.Position == br.BaseStream.Length )
                    {
                        temp.End = pos + 2;
                        text_align = false;
                        break;
                    }
                }

                ReadMesText(br, ref temp, Encoding.Unicode);

                data.keys[i].Value = temp;
            }                
        }

        public void Read(BinaryReader br)
        {
            ReadHeader(br);
            ReadKeys(br);
            ReadValues(br);
        }



        public List<Tuple<string, string>> GetAllValues()
        {
            var result = new List<Tuple<string, string>>();

            foreach (MesKeyValue kv in data.keys)
            {
                result.Add(Tuple.Create<string, string>(kv.Key.LocalStringCopy, kv.Value.LocalStringCopy));
            }

            return result;
        }

        public Tuple<string, string> GetSingleValue(int index)
        {
            if( index < data.keys.Length)
            {
                MesKeyValue item = data.keys[index];

                return Tuple.Create<string, string>(item.Key.LocalStringCopy, item.Value.LocalStringCopy);
            }

            return Tuple.Create<string, string>("", "");
        }

        public bool UpdateValue(int index, string key, string value)
        {
            bool changed = false;

            if (key.Length != 0)
            {
                if (index < data.keys.Length)
                {
                    MesKeyValue item = data.keys[index];

                    // NOTE: if the key changes, update Write()

                    //if (item.Key.LocalStringCopy != key)
                    //{
                    //    item.Key.LocalStringCopy = key;
                    //    item.Dirty = true;
                    //    changed = true;
                    //}

                    if (item.Value.LocalStringCopy != value)
                    {
                        item.Value.LocalStringCopy = value;
                        item.Dirty = true;
                        changed = true;
                    }

                    if( changed )
                    {
                        data.keys[index] = item;
                    }
                }
            }

            return changed;
        }

        public int NumEdits()
        {
            int edit_count = 0;

            foreach (MesKeyValue kv in data.keys)
            {
                if( kv.Dirty == true )
                {
                    ++edit_count;
                }
            }

            return edit_count;
        }
    }
}
