using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;

namespace InstaTransfer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            characterValues = ReadTableFile(System.Windows.Forms.Application.StartupPath + @"\Config\Table.ini");
        }
        OpenFileDialog ofd = new OpenFileDialog();
        string currentROM;
        string BinFile;
        string TRFCode;
        string gameCode;
        string gameName;
        string gameType;
        private uint itemTable;
        private ushort numberOfItems;
        private uint pokemonNamesLocation;
        private ushort numberOfPokemon;
        private uint PokemonStats;
        private uint TypeNames;
        private ushort NumberofTypes;
        private uint TRFpokemonNamesLocation;
        private ushort TRFnumberOfPokemon;
        private uint TRFPokemonStats;
        private Dictionary<byte, char> characterValues;

        //Open Dialog
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ofd.Filter = "GBA ROM (*.gba)|*.gba"; //Opens GBA File
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentROM = ofd.FileName;
                using (BinaryReader br = new BinaryReader(File.OpenRead(currentROM)))
                {
                    br.BaseStream.Seek(0xAC, SeekOrigin.Begin);
                    gameCode = Encoding.ASCII.GetString(br.ReadBytes(4));
                }
                ParseINI(System.IO.File.ReadAllLines(System.Windows.Forms.Application.StartupPath + @"\Config\ROMS.ini"), gameCode);  //Parses ROMS ini to obtain offsets
                string[] gameCodeArray = { "AXVE", "AXPE", "BPRE", "BPGE", "BPEE" };
                if (gameCodeArray.Contains(gameCode))
                {
                    comboBox1.Items.Clear();
                    for (uint i = 0; i <= numberOfPokemon; i++)
                    {
                        if (i <= numberOfPokemon)
                            comboBox1.Items.Add(ROMCharactersToString(10, (uint)(0xB * i + pokemonNamesLocation)));
                    }
                    for (uint i = 0; i <= numberOfItems; i++)
                    {
                        if (i <= numberOfItems)
                            ItemBox.Items.Add(ROMCharactersToString(11, (uint)(44 * i + itemTable)));
                    }
                    StatOffset.Text = "0x" + PokemonStats.ToString("x4");
                    NameOffset.Text = "0x" + pokemonNamesLocation.ToString("x4");
                    ofd.Filter = "InstaTransfer TRF (*.TRF)|*.TRF"; //Opens TRF file
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        BinFile = ofd.FileName;
                        comboBox2.Items.Clear();
                        using (BinaryReader br2 = new BinaryReader(File.OpenRead(BinFile)))
                        {
                            br2.BaseStream.Seek(0xAC, SeekOrigin.Begin);
                            TRFCode = Encoding.ASCII.GetString(br2.ReadBytes(4));
                        }
                        ParseTRFINI(System.IO.File.ReadAllLines(System.Windows.Forms.Application.StartupPath + @"\Config\TRF.ini"), TRFCode);  //Parses TRF ini to obtain offsets
                        for (uint i = 0; i <= TRFnumberOfPokemon; i++)
                        {
                            if (i <= TRFnumberOfPokemon)
                                comboBox2.Items.Add(BinCharactersToString(10, (uint)(0xB * i + TRFpokemonNamesLocation)));
                        }
                    }
                    label66.Text = "Loaded ROM: " + ofd.SafeFileName + " | " + gameName;
                    comboBox2.Enabled = true;
                    comboBox1.Enabled = true;
                    TransferName.Enabled = true;
                    TransferStats.Enabled = true;
                    Next.Enabled = true;
                }
            }
        }

        //Read TRF Pokemon data
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            //This entire thing runs fairly slow, reads entire data in about a second, seems to stall more than it should.
            BinaryReader br = new BinaryReader(File.OpenRead(BinFile));
            //Read Base Stats
            long Offset = TRFPokemonStats + (comboBox2.SelectedIndex * 0x1C);
            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            IHp.Text = "HP " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
            IAtk.Text = "ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
            IDef.Text = "DEF " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
            ISAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            ISDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
            ISpd.Text = "SPD " + Convert.ToString(br.ReadByte());

            //Reads Types
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            byte chunk = br.ReadByte();
            long TypeNames2 = TypeNames + (chunk * 7);
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                IType1.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                IType1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
            chunk = br.ReadByte();
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                IType2.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                IType2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }

            //Catch Rate
            br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
            CatchIni.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
            //EXP Yield
            br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
            IYeild.Text = "Yield: " + Convert.ToString(br.ReadByte());
            //Reads Gender and determines what percential it is
            br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
            string male = "0";
            string female = "254";
            string genderless = "255";
            string half = "31";

            string Gender = Convert.ToString(br.ReadByte());
            if (Gender == male)
            {
                IGender.Text = "100% Male";
            }
            else if (Gender == female)
            {
                IGender.Text = "100% Female";
            }
            else if (Gender == genderless)
            {
                IGender.Text = "Genderless";
            }
            else if (Gender == half)
            {
                IGender.Text = "50/50";
            }
            else if (Gender == "127")
            {
                IGender.Text = "75% Female";
            }
            else
            { IGender.Text = Gender; }

            //Hatch Rate
            br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
            IHatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
            //Base Happiness
            br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
            IHappy.Text = Convert.ToString(br.ReadByte());
            //Exp growth Rate
            br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
            string EXPRate = Convert.ToString(br.ReadByte());
            IRate.Text = GetEXPRate(EXPRate);
            //Egg Group
            br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
            string Egg1 = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
            string Egg2 = Convert.ToString(br.ReadByte());
            //Obtains correct Egg group name to be displayed
            IEgg1.Text = GetEggGroup(Egg1);
            IEgg2.Text = GetEggGroup(Egg2);
            //Abilities
            br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
            IAbil1.Text = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
            IAbil2.Text = Convert.ToString(br.ReadByte());
            //Safari Zone Run Rate
            br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
            ISafari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
            //Color
            br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
            string Cint = Convert.ToString(br.ReadByte());
            IColor.Text = "Color: " + GetColor(Cint);
            //EV
            br.BaseStream.Seek(Offset + 10, SeekOrigin.Begin);
            string Ibyte1 = Convert.ToString(br.ReadByte());

            br.BaseStream.Seek(Offset + 11, SeekOrigin.Begin);
            string Ibyte2 = Convert.ToString(br.ReadByte());

            byte[] bytes = new[] { (byte)Convert.ToInt32(Ibyte1), (byte)Convert.ToInt32(Ibyte2) };

            System.Collections.BitArray array = new System.Collections.BitArray(bytes);
            {
                for (int i = 0, j = 1; i < 11; i = (i + 2), j = (j + 2))
                {
                    if (array[i] == false)
                    {
                        if (array[j] == false)
                        {
                            if (i == 0)
                            {
                                IHPEV.Text = "HP 0";
                            }
                            else if (i == 2)
                            {
                                IAttackEV.Text = "ATK 0";
                            }
                            else if (i == 4)
                            {
                                IDefenseEV.Text = "DEF 0";
                            }
                            else if (i == 6)
                            {
                                ISpeedEV.Text = "SPD 0";
                            }
                            else if (i == 8)
                            {
                                ISpAttackEV.Text = "S.ATK 0";
                            }
                            else if (i == 10)
                            {
                                ISpDefenseEV.Text = "S.DEF 0";
                            }
                        }

                        else
                        {
                            if (i == 0)
                            {
                                IHPEV.Text = "HP 2";
                            }
                            else if (i == 2)
                            {
                                IAttackEV.Text = "ATK 2";
                            }
                            else if (i == 4)
                            {
                                IDefenseEV.Text = "DEF 2";
                            }
                            else if (i == 6)
                            {
                                ISpeedEV.Text = "SPD 2";
                            }
                            else if (i == 8)
                            {
                                ISpAttackEV.Text = "S.ATK 2";
                            }
                            else if (i == 10)
                            {
                                ISpDefenseEV.Text = "S.DEF 2";
                            }
                        }
                    }

                    else
                    {
                        if (array[j] == false)
                        {
                            if (i == 0)
                            {
                                IHPEV.Text = "HP 1";
                            }
                            else if (i == 2)
                            {
                                IAttackEV.Text = "ATK 1";
                            }
                            else if (i == 4)
                            {
                                IDefenseEV.Text = "DEF 1";
                            }
                            else if (i == 6)
                            {
                                ISpeedEV.Text = "SPD 1";
                            }
                            else if (i == 8)
                            {
                                ISpAttackEV.Text = "S.ATK 1";
                            }
                            else if (i == 10)
                            {
                                ISpDefenseEV.Text = "S.DEF 1";
                            }
                        }

                        else
                        {
                            if (i == 0)
                            {
                                IHPEV.Text = "HP 3";
                            }
                            else if (i == 2)
                            {
                                IAttackEV.Text = "ATK 3";
                            }
                            else if (i == 4)
                            {
                                IDefenseEV.Text = "DEF 3";
                            }
                            else if (i == 6)
                            {
                                ISpeedEV.Text = "SPD 3";
                            }
                            else if (i == 8)
                            {
                                ISpAttackEV.Text = "S.ATK 3";
                            }
                            else if (i == 10)
                            {
                                ISpDefenseEV.Text = "S.DEF 3";
                            }
                        }
                    }
                }
            }
            //Item Loading
            #region Items
            int add = 0, index = 0;
            br.BaseStream.Seek(Offset + 12, SeekOrigin.Begin);
            byte[] array1 = br.ReadBytes(2);
            add = array1[1];
            index = array1[0];
            for (; add != 0; add--)
            {
                index = index + 256;
            }
            ItemBox.SelectedIndex = index;
            ITem1.Text = ItemBox.Text;
            br.BaseStream.Seek(Offset + 14, SeekOrigin.Begin);
            byte[] array2 = br.ReadBytes(2);
            add = array2[1];
            index = array2[0];
            for (; add != 0; add--)
            {
                index = index + 256;
            }
            ItemBox.SelectedIndex = index;
            ITem2.Text = ItemBox.Text;
            #endregion
            br.Close();

        }

        //Read ROM Pokemon data
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Reads Base Stats
            BinaryReader br = new BinaryReader(File.OpenRead(currentROM));
            long Offset = PokemonStats + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            Hp.Text = "HP " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
            Atk.Text = "ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
            Def.Text = "DEF " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
            SAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            SDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
            Spd.Text = "SPD " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            //Reads Types
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            byte chunk = br.ReadByte();
            long TypeNames2 = TypeNames + (chunk * 7);
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                Type1.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                Type1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
            chunk = br.ReadByte();
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                Type2.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                Type2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            //Catch Rate
            br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
            Catch.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
            //Exp yield
            br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
            Yield.Text = "Yield: " + Convert.ToString(br.ReadByte());
            //Reds Gender then determines what percentile it falls under
            br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
            string male = "0";
            string female = "254";
            string genderless = "255";
            string half = "31";
            string RGender = Convert.ToString(br.ReadByte());
            if (RGender == male)
            {
                Gender.Text = "100% Male";
            }
            else if (RGender == female)
            {
                Gender.Text = "100% Female";
            }
            else if (RGender == genderless)
            {
                Gender.Text = "Genderless";
            }
            else if (RGender == half)
            {
                Gender.Text = "50/50";
            }
            else if (RGender == "127")
            {
                Gender.Text = "75% Female";
            }
            else
            {
                Gender.Text = RGender;
            }
            //Hatch Rate
            br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
            Hatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
            //Base Happiness
            br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
            Happy.Text = Convert.ToString(br.ReadByte());
            //Exp growth Rate
            br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
            string EXPRate = Convert.ToString(br.ReadByte());
            Rate.Text = GetEXPRate(EXPRate);
            //Egg groups
            br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
            string Egg1 = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
            string Egg2 = Convert.ToString(br.ReadByte());
            //Obtains correct labeling for egg groups
            REgg1.Text = GetEggGroup(Egg1);
            REgg2.Text = GetEggGroup(Egg2);
            //Abilities, idk why but ability 2 doesnt always work
            br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
            Abil1.Text = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
            Abil2.Text = Convert.ToString(br.ReadByte());
            //Safari Zone run rate
            br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
            Safari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
            //Color
            br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
            string Cint = Convert.ToString(br.ReadByte());
            Color.Text = "Color: " + GetColor(Cint);
            //EV
            br.BaseStream.Seek(Offset + 10, SeekOrigin.Begin);
            string byte1 = Convert.ToString(br.ReadByte());

            br.BaseStream.Seek(Offset + 11, SeekOrigin.Begin);
            string byte2 = Convert.ToString(br.ReadByte());

            byte[] bytes = new[] { (byte)Convert.ToInt32(byte1), (byte)Convert.ToInt32(byte2) };

            System.Collections.BitArray array = new System.Collections.BitArray(bytes);
            {
                for (int i = 0, j = 1; i < 11; i = (i + 2), j = (j + 2))
                {
                    if (array[i] == false)
                    {
                        if (array[j] == false)
                        {
                            if (i == 0)
                            {
                                HPEV.Text = "HP 0";
                            }
                            else if (i == 2)
                            {
                                AttackEV.Text = "ATK 0";
                            }
                            else if (i == 4)
                            {
                                DefenseEV.Text = "DEF 0";
                            }
                            else if (i == 6)
                            {
                                SpeedEV.Text = "SPD 0";
                            }
                            else if (i == 8)
                            {
                                SpAttackEV.Text = "S.ATK 0";
                            }
                            else if (i == 10)
                            {
                                SpDefenseEV.Text = "S.DEF 0";
                            }
                        }

                        else
                        {
                            if (i == 0)
                            {
                                HPEV.Text = "HP 2";
                            }
                            else if (i == 2)
                            {
                                AttackEV.Text = "ATK 2";
                            }
                            else if (i == 4)
                            {
                                DefenseEV.Text = "DEF 2";
                            }
                            else if (i == 6)
                            {
                                SpeedEV.Text = "SPD 2";
                            }
                            else if (i == 8)
                            {
                                SpAttackEV.Text = "S.ATK 2";
                            }
                            else if (i == 10)
                            {
                                SpDefenseEV.Text = "S.DEF 2";
                            }
                        }
                    }

                    else
                    {
                        if (array[j] == false)
                        {
                            if (i == 0)
                            {
                                HPEV.Text = "HP 1";
                            }
                            else if (i == 2)
                            {
                                AttackEV.Text = "ATK 1";
                            }
                            else if (i == 4)
                            {
                                DefenseEV.Text = "DEF 1";
                            }
                            else if (i == 6)
                            {
                                SpeedEV.Text = "SPD 1";
                            }
                            else if (i == 8)
                            {
                                SpAttackEV.Text = "S.ATK 1";
                            }
                            else if (i == 10)
                            {
                                SpDefenseEV.Text = "S.DEF 1";
                            }
                        }

                        else
                        {
                            if (i == 0)
                            {
                                HPEV.Text = "HP 3";
                            }
                            else if (i == 2)
                            {
                                AttackEV.Text = "ATK 3";
                            }
                            else if (i == 4)
                            {
                                DefenseEV.Text = "DEF 3";
                            }
                            else if (i == 6)
                            {
                                SpeedEV.Text = "SPD 3";
                            }
                            else if (i == 8)
                            {
                                SpAttackEV.Text = "S.ATK 3";
                            }
                            else if (i == 10)
                            {
                                SpDefenseEV.Text = "S.DEF 3";
                            }
                        }
                    }
                }
            }
            //Item Loading
            #region Items
            int add = 0, index = 0;
            br.BaseStream.Seek(Offset + 12, SeekOrigin.Begin);
            byte[] array1 = br.ReadBytes(2);
            add = array1[1];
            index = array1[0];
            for (; add != 0; add--)
            {
                index = index + 256;
            }
            ItemBox.SelectedIndex = index;
            RItem1.Text = ItemBox.Text;
            br.BaseStream.Seek(Offset + 14, SeekOrigin.Begin);
            byte[] array2 = br.ReadBytes(2);
            add = array2[1];
            index = array2[0];
            for (; add != 0; add--)
            {
                index = index + 256;
            }
            ItemBox.SelectedIndex = index;
            RItem2.Text = ItemBox.Text;
            #endregion
            br.Close();
        }

        //Transfer Stats from TRF to ROM
        private void TransferStats_Click(object sender, EventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(currentROM));
            BinaryReader br = new BinaryReader(File.OpenRead(BinFile));
            uint vOut1 = Convert.ToUInt32(TRFPokemonStats);
            uint vOut2 = Convert.ToUInt32(PokemonStats);
            long Offset = vOut1 + (comboBox2.SelectedIndex * 28);
            long Offset2 = vOut2 + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Position = Offset;
            bw.BaseStream.Position = Offset2;
            byte[] BytesToWrite = br.ReadBytes(0x1C);
            bw.Write(BytesToWrite);
            bw.Close();
            br.Close();

            //Reloads stats
            //Reads Base Stats
            br = new BinaryReader(File.OpenRead(currentROM));
            Offset = PokemonStats + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            Hp.Text = "HP " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 1, SeekOrigin.Begin);
            Atk.Text = "ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 2, SeekOrigin.Begin);
            Def.Text = "DEF " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 3, SeekOrigin.Begin);
            SAtk.Text = "S.ATK " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            SDef.Text = "S.Def " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 5, SeekOrigin.Begin);
            Spd.Text = "SPD " + Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            //Reads Types
            br.BaseStream.Seek(Offset + 6, SeekOrigin.Begin);
            byte chunk = br.ReadByte();
            long TypeNames2 = TypeNames + (chunk * 7);
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                Type1.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                Type1.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }
            br.BaseStream.Seek(Offset + 7, SeekOrigin.Begin);
            chunk = br.ReadByte();
            //Checks to see if the current ROM supports the needed Type name
            if (chunk > NumberofTypes)
            {
                TypeNames2 = TypeNames + (chunk * 7);
                Type2.Text = "Unavailable";
            }
            else
            {
                //Obtains the Type Names from ROM
                TypeNames2 = TypeNames + (chunk * 7);
                Type2.Text = ROMCharactersToString(7, (uint)(TypeNames2));
            }

            //Catch Rate
            br.BaseStream.Seek(Offset + 8, SeekOrigin.Begin);
            Catch.Text = "Catch Rate: " + Convert.ToString(br.ReadByte());
            //Exp yield
            br.BaseStream.Seek(Offset + 9, SeekOrigin.Begin);
            Yield.Text = "Yield: " + Convert.ToString(br.ReadByte());
            //Reds Gender then determines what percentile it falls under
            br.BaseStream.Seek(Offset + 16, SeekOrigin.Begin);
            string male = "0";
            string female = "254";
            string genderless = "255";
            string half = "31";
            string RGender = Convert.ToString(br.ReadByte());
            if (RGender == male)
            {
                Gender.Text = "100% Male";
            }
            else if (RGender == female)
            {
                Gender.Text = "100% Female";
            }
            else if (RGender == genderless)
            {
                Gender.Text = "Genderless";
            }
            else if (RGender == half)
            {
                Gender.Text = "50/50";
            }
            else if (RGender == "127")
            {
                Gender.Text = "75% Female";
            }
            else
            {
                Gender.Text = RGender;
            }
            //Hatch Rate
            br.BaseStream.Seek(Offset + 17, SeekOrigin.Begin);
            Hatch.Text = "Hatch Rate: " + Convert.ToString(br.ReadByte());
            //Base Happiness
            br.BaseStream.Seek(Offset + 18, SeekOrigin.Begin);
            Happy.Text = Convert.ToString(br.ReadByte());
            //Exp growth Rate
            br.BaseStream.Seek(Offset + 19, SeekOrigin.Begin);
            string EXPRate = Convert.ToString(br.ReadByte());
            Rate.Text = GetEXPRate(EXPRate);
            //Egg groups
            br.BaseStream.Seek(Offset + 20, SeekOrigin.Begin);
            string Egg1 = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 21, SeekOrigin.Begin);
            string Egg2 = Convert.ToString(br.ReadByte());
            //Obtains correct labeling for egg groups
            REgg1.Text = GetEggGroup(Egg1);
            REgg2.Text = GetEggGroup(Egg2);
            //Abilities, idk why but ability 2 doesnt always work
            br.BaseStream.Seek(Offset + 22, SeekOrigin.Begin);
            Abil1.Text = Convert.ToString(br.ReadByte());
            br.BaseStream.Seek(Offset + 23, SeekOrigin.Begin);
            Abil2.Text = Convert.ToString(br.ReadByte());
            //Safari Zone run rate
            br.BaseStream.Seek(Offset + 24, SeekOrigin.Begin);
            Safari.Text = "Safari Zone Rate: " + Convert.ToString(br.ReadByte());
            //Color
            br.BaseStream.Seek(Offset + 25, SeekOrigin.Begin);
            string Cint = Convert.ToString(br.ReadByte());
            Color.Text = "Color: " + GetColor(Cint);
            br.Close();
        }

        //Transfer Name from TRF to ROM
        private void TransferName_Click(object sender, EventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(currentROM));
            BinaryReader br = new BinaryReader(File.OpenRead(BinFile));
            uint vOut1 = Convert.ToUInt32(TRFpokemonNamesLocation);
            uint vOut2 = Convert.ToUInt32(pokemonNamesLocation);
            long Offset = vOut1 + (comboBox2.SelectedIndex * 11);
            long Offset2 = vOut2 + (comboBox1.SelectedIndex * 11);
            br.BaseStream.Position = Offset;
            bw.BaseStream.Position = Offset2;
            int Index = comboBox1.SelectedIndex;
            byte[] BytesToWrite = br.ReadBytes(0xB);
            bw.Write(BytesToWrite);
            br.Close();
            bw.Close();
            //Reloads Names
            comboBox1.Items.Clear();
            for (uint i = 0; i <= numberOfPokemon; i++)
            {
                if (i <= 0x1FE)
                    comboBox1.Items.Add(ROMCharactersToString(10, (uint)(0xB * i + pokemonNamesLocation)));
            }
            comboBox1.SelectedIndex = Index;

        }

        //Transfer Both Name and stats while moving to next Pokemon
        private void Next_Click(object sender, EventArgs e)
        {
            //Transfer stats
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(currentROM));
            BinaryReader br = new BinaryReader(File.OpenRead(BinFile));
            uint vOut1 = Convert.ToUInt32(TRFPokemonStats);
            uint vOut2 = Convert.ToUInt32(PokemonStats);
            long Offset = vOut1 + (comboBox2.SelectedIndex * 28);
            long Offset2 = vOut2 + (comboBox1.SelectedIndex * 28);
            br.BaseStream.Position = Offset;
            bw.BaseStream.Position = Offset2;
            byte[] BytesToWrite = br.ReadBytes(0x1C);
            bw.Write(BytesToWrite);
            //Transfer name
            vOut1 = Convert.ToUInt32(TRFpokemonNamesLocation);
            vOut2 = Convert.ToUInt32(pokemonNamesLocation);
            Offset = vOut1 + (comboBox2.SelectedIndex * 11);
            Offset2 = vOut2 + (comboBox1.SelectedIndex * 11);
            int test = comboBox1.SelectedIndex;
            br.BaseStream.Position = Offset;
            bw.BaseStream.Position = Offset2;
            BytesToWrite = br.ReadBytes(0xB);
            bw.Write(BytesToWrite);
            br.Close();
            bw.Close();
            //Reloads Names
            comboBox1.Items.Clear();
            for (uint i = 0; i <= numberOfPokemon; i++)
            {
                if (i <= numberOfPokemon)
                    comboBox1.Items.Add(ROMCharactersToString(10, (uint)(0xB * i + pokemonNamesLocation)));
            }
            //Next pokemon
            comboBox1.SelectedIndex = (test + 1);
            comboBox2.SelectedIndex = (comboBox2.SelectedIndex + 1);
            
        }





        //INI Parsing Functions

        //Being Diegoisawesome functions, most of this wouldn't be possible without these functions!
        private void ParseINI(string[] iniFile, string romCode)
        {
            bool getValues = false;
            foreach (string s in iniFile)
            {
                if (s.Equals("[" + romCode + "]"))
                {
                    getValues = true;
                    continue;
                }
                if (getValues)
                {
                    if (s.Equals(@"[/" + romCode + "]"))
                    {
                        break;
                    }
                    else
                    {
                        if (s.StartsWith("GameName"))
                        {
                            gameName = s.Split('=')[1];
                        }
                        if (s.StartsWith("GameType"))
                        {
                            gameType = s.Split('=')[1];
                        }
                        if (s.StartsWith("ItemData"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out itemTable);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out itemTable);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("PokemonStats"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out PokemonStats);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out PokemonStats);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("TypeNames"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out TypeNames);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out TypeNames);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberOfItems"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out numberOfItems);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out numberOfItems);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of items.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberofTypes"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out NumberofTypes);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out NumberofTypes);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of items.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("PokemonNames"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out pokemonNamesLocation);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out pokemonNamesLocation);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the Pokémon names offset.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberOfPokemon"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out numberOfPokemon);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out numberOfPokemon);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of Pokémon.");
                                    break;
                                }

                                if (!getValues)
                                {
                                    gameCode = "Unknown";
                                    gameName = "Unknown ROM";
                                }
                            }
                        }
                    }
                }
            }
        }
        private void ParseTRFINI(string[] iniFile, string romCode)
        {
            bool getValues = false;
            foreach (string s in iniFile)
            {
                if (s.Equals("[" + romCode + "]"))
                {
                    getValues = true;
                    continue;
                }
                if (getValues)
                {
                    if (s.Equals(@"[/" + romCode + "]"))
                    {
                        break;
                    }
                    else
                    {
                        if (s.StartsWith("PokemonStats"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out TRFPokemonStats);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out TRFPokemonStats);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the item names location.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("PokemonNames"))
                        {
                            bool success = UInt32.TryParse(s.Split('=')[1], out TRFpokemonNamesLocation);
                            if (!success)
                            {
                                success = UInt32.TryParse(ToDecimal(s.Split('=')[1]), out TRFpokemonNamesLocation);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the Pokémon names offset.");
                                    break;
                                }
                            }
                        }
                        else if (s.StartsWith("NumberOfPokemon"))
                        {
                            bool success = UInt16.TryParse(s.Split('=')[1], out TRFnumberOfPokemon);
                            if (!success)
                            {
                                success = UInt16.TryParse(ToDecimal(s.Split('=')[1]), out TRFnumberOfPokemon);
                                if (!success)
                                {
                                    MessageBox.Show("There was an error parsing the value for the number of Pokémon.");
                                    break;
                                }

                                if (!getValues)
                                {
                                    gameCode = "Unknown TRF";
                                    gameName = "Unknown TRF";
                                }
                            }
                        }
                    }
                }
            }
        }
        public string ToDecimal(string input)
        {
            if (input.ToLower().StartsWith("0x") || input.ToUpper().StartsWith("&H"))
            {
                return Convert.ToUInt32(input.Substring(2), 16).ToString();
            }
            else if (input.ToLower().StartsWith("0o"))
            {
                return Convert.ToUInt32(input.Substring(2), 8).ToString();
            }
            else if (input.ToLower().StartsWith("0b"))
            {
                return Convert.ToUInt32(input.Substring(2), 2).ToString();
            }
            else if (input.ToLower().StartsWith("0t"))
            {
                return ThornalToDecimal(input.Substring(2));
            }
            else if ((input.StartsWith("[") && input.EndsWith("]")) || (input.StartsWith("{") && input.EndsWith("}")))
            {
                return Convert.ToUInt32(input.Substring(1, (input.Length - 2)), 2).ToString();
            }
            else if (input.ToLower().EndsWith("h"))
            {
                return Convert.ToUInt32(input.Substring(0, (input.Length - 1)), 16).ToString();
            }
            else if (input.ToLower().EndsWith("b"))
            {
                return Convert.ToUInt32(input.Substring(0, (input.Length - 1)), 2).ToString();
            }
            else if (input.ToLower().EndsWith("t"))
            {
                return ThornalToDecimal(input.Substring(0, (input.Length - 1)));
            }
            else if (input.StartsWith("$"))
            {
                return Convert.ToUInt32(input.Substring(1), 16).ToString();
            }
            else
            {
                return Convert.ToUInt32(input, 16).ToString();
            }
        }
        private string ThornalToDecimal(string input)
        {
            uint total = 0;
            char[] temp = input.ToCharArray();
            for (int i = input.Length - 1; i >= 0; i--)
            {
                int value = 0;
                bool success = Int32.TryParse(temp[i].ToString(), out value);
                if (!success)
                {
                    if (temp[i] < 'W' && temp[i] >= 'A')
                    {
                        value = temp[i] - 'A' + 10;
                    }
                    else
                    {
                        throw new FormatException(temp[i] + " is an invalid character in the Base 32 number set.");
                    }
                }
                total += (uint)(Math.Pow((double)32, (double)(input.Length - 1 - i)) * value);
            }
            return total.ToString();
        }
        private string ROMCharactersToString(int maxLength, uint baseLocation)
        {
            string s = "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(currentROM)))
            {
                for (int j = 0; j < maxLength; j++)
                {
                    br.BaseStream.Seek(baseLocation + j, SeekOrigin.Begin);
                    byte textByte = br.ReadByte();
                    if ((textByte != 0xFF))
                    {
                        char temp = ';';
                        bool success = characterValues.TryGetValue(textByte, out temp);
                        s += temp;
                        if (!success)
                        {
                            if (textByte == 0x53)
                            {
                                s = s.Substring(0, s.Length - 1) + "PK";
                            }
                            else if (textByte == 0x54)
                            {
                                s = s.Substring(0, s.Length - 1) + "MN";
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return s;
        }
        private string BinCharactersToString(int maxLength, long baseLocation)
        {
            string s = "";
            using (BinaryReader br = new BinaryReader(File.OpenRead(BinFile)))
            {
                for (int j = 0; j < maxLength; j++)
                {
                    br.BaseStream.Seek(baseLocation + j, SeekOrigin.Begin);
                    byte textByte = br.ReadByte();
                    if ((textByte != 0xFF))
                    {
                        char temp = ';';
                        bool success = characterValues.TryGetValue(textByte, out temp);
                        s += temp;
                        if (!success)
                        {

                            if (textByte == 0x53)
                            {
                                s = s.Substring(0, s.Length - 1) + "PK";
                            }
                            else if (textByte == 0x54)
                            {
                                s = s.Substring(0, s.Length - 1) + "MN";
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return s;
        }
        private Dictionary<byte, char> ReadTableFile(string iniLocation)
        {
            Dictionary<byte, char> characterValues = new Dictionary<byte, char>();
            string[] tableFile = System.IO.File.ReadAllLines(iniLocation);
            int index = 0;
            foreach (string s in tableFile)
            {
                if (!s.Equals("") && !s.Equals("[Table]") && index != 0x9E && index != 0x9F)
                {
                    string[] stuff = s.Split('=');
                    switch (Byte.Parse(ToDecimal("0x" + stuff[0])))
                    {
                        case 0:
                            characterValues.Add(0, ' ');
                            break;
                        case 0x34:
                            break;
                        case 0x35:
                            characterValues.Add(0x35, '=');
                            break;
                        case 0x53:
                            break;
                        case 0x54:
                            break;
                        case 0x55:
                            break;
                        case 0x56:
                            break;
                        case 0x57:
                            break;
                        case 0x58:
                            break;
                        case 0x59:
                            break;
                        case 0x79:
                            break;
                        case 0x7A:
                            break;
                        case 0x7B:
                            break;
                        case 0x7C:
                            break;
                        case 0xB0:
                            break;
                        case 0xEF:
                            break;
                        case 0xF7:
                            break;
                        case 0xF8:
                            break;
                        case 0xF9:
                            break;
                        case 0xFA:
                            break;
                        case 0xFB:
                            break;
                        case 0xFC:
                            break;
                        case 0xFD:
                            break;
                        case 0xFE:
                            break;
                        case 0xFF:
                            break;
                        default:
                            characterValues.Add(Byte.Parse(ToDecimal("0x" + stuff[0])), stuff[1].ToCharArray()[0]);
                            break;
                    }
                    index++;
                }
            }
            return characterValues;
        }
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars];
            using (var sr = new StringReader(hex))
            {
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] =
                      Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            }
            return bytes;
        }
        //End of Diegoisawesome credits

        public string GetEggGroup(string Egg)
        {
            if (Egg == "1")
            {
                Egg = "Monster";
            }
            else if (Egg == "2")
            {
                Egg = "Water 1";
            }
            else if (Egg == "3")
            {
                Egg = "Bug";
            }
            else if (Egg == "4")
            {
                Egg = "Flying";
            }
            else if (Egg == "5")
            {
                Egg = "Field";
            }
            else if (Egg == "6")
            {
                Egg = "Fairy";
            }
            else if (Egg == "7")
            {
                Egg = "Grass";
            }
            else if (Egg == "8")
            {
                Egg = "Human-Like";
            }
            else if (Egg == "9")
            {
                Egg = "Water 3";
            }
            else if (Egg == "10")
            {
                Egg = "Mineral";
            }
            else if (Egg == "11")
            {
                Egg = "Amorphous";
            }
            else if (Egg == "12")
            {
                Egg = "Water 2";
            }
            else if (Egg == "13")
            {
                Egg = "Ditto";
            }
            else if (Egg == "14")
            {
                Egg = "Dragon";
            }
            else if (Egg == "15")
            {
                Egg = "Undiscovered";
            }
            return Egg;
        }
        public string GetEXPRate(string Exp)
        {
            if (Exp == "0")
            {
                Exp = "Medium Fast";
            }
            else if (Exp == "1")
            {
                Exp = "Erratic";
            }
            else if (Exp == "2")
            {
                Exp = "Fluctuating";
            }
            else if (Exp == "3")
            {
                Exp = "Medium Slow";
            }
            else if (Exp == "4")
            {
                Exp = "Fast";
            }
            else if (Exp == "5")
            {
                Exp = "Slow";
            }
            return Exp;
        }
        public string GetColor(string Color)
        {
            if (Color == "0")
            {
                Color = "Red";
            }
            else if (Color == "1")
            {
                Color = "Blue";
            }
            else if (Color == "2")
            {
                Color = "Yellow";
            }
            else if (Color == "3")
            {
                Color = "Green";
            }
            else if (Color == "4")
            {
                Color = "Black";
            }
            else if (Color == "5")
            {
                Color = "Brown";
            }
            else if (Color == "6")
            {
                Color = "Purple";
            }
            else if (Color == "7")
            {
                Color = "Gray";
            }
            else if (Color == "8")
            {
                Color = "White";
            }
            else if (Color == "9")
            {
                Color = "Pink";
            }
            return Color;
        }

        //Help dialogs
        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                string fileLoc = System.Windows.Forms.Application.StartupPath + @"\Config\Readme.rtf";
                Process.Start(fileLoc);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show("Could not open Readme.rtf." + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void iNIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string fileLoc = System.Windows.Forms.Application.StartupPath + @"\Config\ROMS.ini";
                Process.Start(fileLoc);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show("Could not open ROMS.ini. This is a needed file for the program to work!" + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("IPTS is a program created by Joexv to make inserting Pokemon so much easier. No longer will you have to manually open a Pokemon Editor to one by one add gen 4 Pokemon. A few clicks with IPTS and you've got yourself all the stats you need to make your game the best game it can be!");
        }
        private void tRFFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("A TRF file, or Transfer Reading Filesystem, is a custom made file to work with IPTS without the need of bulky ini files or removing any features. Because of the design of the TRF file, it is 100% future proof. If any new generation of Pokemon are added, you can simply edit them in. That simple!");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
