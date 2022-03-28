using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Drawing.Drawing2D;
using System.IO;

namespace GraphicalRisk
{
    public partial class Form1 : Form
    {
        #region Global Variables
        public static Random rand;
        public static List<Territory> world;
        public static List<TextBox> troopCounters;
        public static string[] playerNames;
        public static Color[] playerColors;
        public static Color[] highlightColors;
        public static string[] continents;
        public static int players;
        public static bool blitzTesting;
        public static int turn;
        public static bool maxMoveMemory;
        public const string password = "afghanistan";
        public static int passwordProgress;

        // 0: adjacent, 1: path, 2: anywhere
        public const int fortifyMode = 0;

        // the ID of the currently selected territory
        public static int selection;

        // memory for attack phase
        public static int selectionA;
        public static int selectionB;

        // this only exists for manually deciding
        // who gets what
        public static int territoriesTaken;

        // ditto, but for drafting
        public static int armiesEach;
        public static int armiesPlaced;

        public static int soundID;
        public static System.Media.SoundPlayer click;
        public static System.Media.SoundPlayer hit;
        public static System.Media.SoundPlayer die;
        public static System.Media.SoundPlayer explode;

        public enum Ter : int
        {
            Alaska,
            Alberta,
            CentralAmerica,
            EasternUnitedStates,
            Greenland,
            NorthwestTerritory,
            Ontario,
            Quebec,
            WesternUnitedStates,
            Argentina,
            Brazil,
            Peru,
            Venezuela,
            GreatBritain,
            Iceland,
            NorthernEurope,
            Scandinavia,
            SouthernEurope,
            Ukraine,
            WesternEurope,
            Congo,
            EastAfrica,
            Egypt,
            Madagascar,
            NorthAfrica,
            SouthAfrica,
            Afghanistan,
            China,
            India,
            Irkutsk,
            Japan,
            Kamchatka,
            MiddleEast,
            Mongolia,
            Siam,
            Siberia,
            Ural,
            Yakutsk,
            EasternAustralia,
            Indonesia,
            NewGuinea,
            WesternAustralia,
        }
        public enum Status
        {
            // asking how many players there are
            NumPlayers,

            // asking whether to assign territories
            // to players automatically, or the
            // old-fashioned way
            WhichClaim,

            // doing it automatically
            AutoClaim,

            // doing it manually
            ManualClaim,

            // asking whether to assign troops to
            // starting territories automatically
            WhichStartArmy,

            // doing it automatically
            AutoStartArmy,

            // doing it manually
            ManualStartArmy,
            DraftPhase,
            AttackPhase,
            FortifyPhase,
            GameOver
        }
        public static Status status;
        public static string[] terArray;
        #endregion
        public Form1()
        {
            InitializeComponent();
            Worldbuilding();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // This is basically Main() now, so...
            // TODO:
            // Cards (should be hard to do badly and impossible to do well)
            // Visible dice (I don't even know)

            // TODONE:
            // the game notices when you use the max size
            // invasion force and defaults to it instead of 1
            //
            // fortifying works properly now
            dummy.Focus();
            bool playClick = true;
            #region find out what was clicked
            var ev = e as MouseEventArgs;
            selection = findCountry(ev.X, ev.Y);
            if (selection != -1)
            {
                selectedTerritory.Text = world[selection].name;
                //click.Play();
            }
            else
            { 
                selectedTerritory.Text = "Unknown/Ocean";
                playClick = false;
            }

            if (sender != pictureBox1)
            {
                var sv = sender as Control;
                string countryName = sv.Name;
                for (int i = 0; i < 42; i++)
                {
                    if (terArray[i] == countryName)
                    {
                        selection = i;
                        selectedTerritory.Text = world[i].name;
                    }
                }
            }
            #endregion

            #region Manual Claim
            if (status == Status.ManualClaim && selection != -1)
            {
                if (world[selection].player == "Neutral")
                {
                    world[selection].Conquer(playerNames[turn], 1, 
                        playerColors[turn]);
                    turn++;
                    turn %= players;
                    PromptBox.Text = "It is the " + playerNames[turn] + 
                        " player's turn.";
                    territoriesTaken++;
                }
            }
            if (status == Status.ManualClaim && territoriesTaken == 42)
            {
                status = Status.WhichStartArmy;
                PromptBox.Text = "Automatically place armies?";
                Show(ManualButton);
                Show(AutoButton);
            }
            #endregion

            #region Manual Start Army
            if (status == Status.ManualStartArmy && selection != -1)
            {
                if (world[selection].player == playerNames[turn])
                {
                    world[selection].Draft(1);
                    turn++;
                    turn %= players;
                    PromptBox.Text = "It is the " + playerNames[turn] +
                        " player's turn";
                    armiesPlaced++;
                }
            }
            if (status == Status.ManualStartArmy && armiesPlaced == armiesEach * players)
            {
                status = Status.DraftPhase;
                PromptBox.Text = "It is the " + playerNames[turn] + " player's turn";
                Show(PromptBox2);

                // reusing this global variable
                // because I already have too many
                armiesEach = DraftableArmies();
                PromptBox2.Text = armiesEach + " armies left";
            }
            #endregion

            dummy.Focus();

            #region Draft Phase
            if (status == Status.DraftPhase && selection != -1)
            {
                if (world[selection].player == playerNames[turn])
                {
                    armiesEach--;
                    world[selection].Draft(1);
                    if (armiesEach > 1)
                        PromptBox2.Text = armiesEach + " armies left";
                    if (armiesEach == 1)
                        PromptBox2.Text = "1 army left";
                    if (armiesEach < 1)
                    {
                        status = Status.AttackPhase;
                        selectedTerritory.Text = " ";
                        //PromptBox2.Size = new Size(260, 31);
                        PromptBox2.Text = "Select where to attack from";
                        Show(StopAttButton);
                        dummy.Focus();
                        if (playClick)
                            click.Play();
                        return;
                    }
                }
                else playClick = false;
            }
            #endregion

            #region Attack Phase
            if (status == Status.AttackPhase && selection != -1)
            {
                // pick which territory will attack
                if (selectionA == -1 && world[selection].player == playerNames[turn]
                    && world[selection].armies > 1)
                {
                    selectionA = selection;
                    Highlight(selectionA);
                    PromptBox2.Text = "Select a territory to attack";
                }

                if (selectionA == -1 && world[selection].player != playerNames[turn])
                    playClick = false;

                // pick a different territory to attack with
                if (selectionA != -1 && selectionB == -1
                    && world[selection].player == playerNames[turn]
                    && world[selection].armies > 1)
                {
                    Unlight(selectionA);
                    selectionA = selection;
                    Highlight(selectionA);
                }

                // pick which territory will be attacked
                if (selectionA != -1 && selectionB == -1
                    && world[selection].player != playerNames[turn]
                    && world[selectionA].connections.Contains(world[selection]))
                {
                    selectionB = selection;
                    Highlight(selectionB);
                    Hide(PromptBox2);
                    Show(AttackButton);
                    Show(BlitzButton);
                    Show(ResetAttButton);
                    Hide(StopAttButton);
                }

                if (selectionA != -1 && selectionB == -1
                    && world[selection].player != playerNames[turn]
                    && !world[selectionA].connections.Contains(world[selection]))
                {
                    playClick = false;
                }
                    // manifest some attack buttons

                    // attacking continues until either:
                    //   1: the player gives up
                    //        reset stuff and start over
                    //   2: the player runs out of attackers
                    //        reset stuff and start over
                    //   3: the player wins
                    //        ask how many armies should
                    //        occupy the defeated territory
                    //
                    // I want to add a 1 button and a max button
                    // to set the input box to those values
                    dummy.Focus();
            }
            #endregion

            #region Fortify Phase
            if (status == Status.FortifyPhase && selection != -1)
            {
                // pick which territory will send troops
                if (selectionA == -1 && world[selection].player == playerNames[turn]
                    && world[selection].armies > 1)
                {
                    selectionA = selection;
                    Highlight(selectionA);
                    PromptBox2.Text = "Where will you send troops?";
                }
                else if (selectionB == -1 && selection == selectionA)
                {
                    // deselect/cancel
                    Unlight(selectionA);
                    selectionA = -1;
                    PromptBox2.Text = "Pick where to move troops from";
                    selectedTerritory.Text = " ";
                }
                else if (selectionB == -1 && world[selection].player == playerNames[turn]
                    && !Pathfinder(selectionA, selection))
                {
                    // pick a different territory to send troops
                    Unlight(selectionA);
                    selectionA = selection;
                    Highlight(selectionA);
                    PromptBox2.Text = "Where will you send troops?";
                }
                else if (selectionB == -1 && world[selection].player == playerNames[turn]
                    && Pathfinder(selectionA, selection))
                {
                    selectionB = selection;
                    Highlight(selectionB);
                    PromptBox2.Text = "How many will you send?";
                    Show(FortifyInput);
                    Show(FortOne);
                    Show(FortHalf);
                    Show(FortMax);
                    Show(FortConfButton);
                    FortifyInput.Maximum = world[selectionA].armies - 1;
                    if (maxMoveMemory)
                        FortifyInput.Value = FortifyInput.Maximum;
                }
                else if (selectionA != -1 && selectionB != -1 
                    && world[selection].player == playerNames[turn])
                {
                    Unlight(selectionA);
                    Unlight(selectionB);
                    Hide(FortifyInput);
                    Hide(FortOne);
                    Hide(FortHalf);
                    Hide(FortMax);
                    Hide(FortConfButton);
                    selectionA = -1;
                    selectionB = -1;
                    PromptBox2.Text = "Pick where to move troops from";
                    selectedTerritory.Text = " ";
                }
            }
            else if (status == Status.FortifyPhase && selection == -1)
            {
                Unlight(selectionA);
                Unlight(selectionB);
                Hide(FortifyInput);
                Hide(FortOne);
                Hide(FortHalf);
                Hide(FortMax);
                Hide(FortConfButton);
                selectionA = -1;
                selectionB = -1;
                PromptBox2.Text = "Pick where to move troops from";
                selectedTerritory.Text = " ";
            }
            #endregion
            // advance turn where???
            // don't forget to poke
            // players to see if they
            // are still alive
            if (playClick && status != Status.WhichClaim
                && status != Status.WhichStartArmy
                && status != Status.GameOver
                && status != Status.NumPlayers)
                click.Play();
        }

        void Worldbuilding()
        {
            click = new System.Media.SoundPlayer("Resources/click3.wav");
            hit = new System.Media.SoundPlayer("Resources/hit1.wav");
            die = new System.Media.SoundPlayer("Resources/fallbig.wav");
            explode = new System.Media.SoundPlayer("Resources/explode3.wav");
            world = new List<Territory>();
            troopCounters = new List<TextBox>();
            rand = new Random();
            turn = 0;
            selectionA = -1;
            selectionB = -1;
            territoriesTaken = 0;
            maxMoveMemory = false;
            FortifyInput.Minimum = 1;
            passwordProgress = 0;
            soundID = 0;
            armiesPlaced = 0;
            armiesEach = 20;
            if (players == 5)
                armiesEach = 25;
            if (players == 4)
                armiesEach = 30;
            if (players == 3)
                armiesEach = 35;
            if (players == 2)
                armiesEach = 50;

            #region listOfTerritories
            world.Add(new Territory("Alaska", "North America"));
            world.Add(new Territory("Alberta", "North America"));
            world.Add(new Territory("Central America", "North America"));
            world.Add(new Territory("Eastern United States", "North America"));
            world.Add(new Territory("Greenland", "North America"));
            world.Add(new Territory("Northwest Territory", "North America"));
            world.Add(new Territory("Ontario", "North America"));
            world.Add(new Territory("Quebec", "North America"));
            world.Add(new Territory("Western United States", "North America"));
            world.Add(new Territory("Argentina", "South America"));
            world.Add(new Territory("Brazil", "South America"));
            world.Add(new Territory("Peru", "South America"));
            world.Add(new Territory("Venezuela", "South America"));
            world.Add(new Territory("Great Britain", "Europe"));
            world.Add(new Territory("Iceland", "Europe"));
            world.Add(new Territory("Northern Europe", "Europe"));
            world.Add(new Territory("Scandinavia", "Europe"));
            world.Add(new Territory("Southern Europe", "Europe"));
            world.Add(new Territory("Ukraine", "Europe"));
            world.Add(new Territory("Western Europe", "Europe"));
            world.Add(new Territory("Congo", "Africa"));
            world.Add(new Territory("East Africa", "Africa"));
            world.Add(new Territory("Egypt", "Africa"));
            world.Add(new Territory("Madagascar", "Africa"));
            world.Add(new Territory("North Africa", "Africa"));
            world.Add(new Territory("South Africa", "Africa"));
            world.Add(new Territory("Afghanistan", "Asia"));
            world.Add(new Territory("China", "Asia"));
            world.Add(new Territory("India", "Asia"));
            world.Add(new Territory("Irkutsk", "Asia"));
            world.Add(new Territory("Japan", "Asia"));
            world.Add(new Territory("Kamchatka", "Asia"));
            world.Add(new Territory("Middle East", "Asia"));
            world.Add(new Territory("Mongolia", "Asia"));
            world.Add(new Territory("Siam", "Asia"));
            world.Add(new Territory("Siberia", "Asia"));
            world.Add(new Territory("Ural", "Asia"));
            world.Add(new Territory("Yakutsk", "Asia"));
            world.Add(new Territory("Eastern Australia", "Australia"));
            world.Add(new Territory("Indonesia", "Australia"));
            world.Add(new Territory("New Guinea", "Australia"));
            world.Add(new Territory("Western Australia", "Australia"));
            #endregion

            #region borders
            // Alaska
            world[0].Build(102, 102);
            world[0].Build(142, 72);
            world[0].Build(70, 70);
            world[0].Build(25, 111);

            // Alberta
            world[1].Build(126, 113);
            world[1].Build(214, 113);
            world[1].Build(197, 148);
            world[1].Build(125, 147);

            // Central America
            world[2].Build(102, 231);
            world[2].Build(166, 265);
            world[2].Build(232, 322);
            world[2].Build(124, 295);

            // Eastern United States
            world[3].Build(210, 157);
            world[3].Build(262, 184);
            world[3].Build(306, 167);
            world[3].Build(236, 246);
            world[3].Build(162, 232);
            world[3].Build(164, 211);
            world[3].Build(199, 209);

            // Greenland
            world[4].Build(392, 40);
            world[4].Build(515, 32);
            world[4].Build(420, 100);

            // Northwest Territory
            world[5].Build(154, 72);
            world[5].Build(296, 72);
            world[5].Build(252, 103);
            world[5].Build(113, 102);

            // Ontario
            world[6].Build(225, 110);
            world[6].Build(250, 110);
            world[6].Build(272, 137);
            world[6].Build(262, 163);
            world[6].Build(224, 148);
            world[6].Build(202, 150);

            // Quebec
            world[7].Build(282, 142);
            world[7].Build(317, 103);
            world[7].Build(363, 132);
            world[7].Build(308, 156);
            world[7].Build(292, 167);
            world[7].Build(273, 161);

            // Western United States
            world[8].Build(124, 154);
            world[8].Build(197, 156);
            world[8].Build(192, 201);
            world[8].Build(158, 203);
            world[8].Build(151, 223);
            world[8].Build(85, 219);

            // Argentina
            world[9].Build(278, 466);
            world[9].Build(346, 519);
            world[9].Build(316, 575);
            world[9].Build(291, 572);

            // Brazil
            world[10].Build(265, 398);
            world[10].Build(298, 359);
            world[10].Build(411, 396);
            world[10].Build(360, 491);
            world[10].Build(290, 408);

            // Peru
            world[11].Build(232, 365);
            world[11].Build(256, 381);
            world[11].Build(249, 400);
            world[11].Build(263, 417);
            world[11].Build(282, 419);
            world[11].Build(330, 475);
            world[11].Build(272, 446);
            world[11].Build(219, 394);

            // Venezuela
            world[12].Build(266, 312);
            world[12].Build(341, 345);
            world[12].Build(301, 345);
            world[12].Build(264, 372);
            world[12].Build(235, 360);

            // Great Britain
            world[13].Build(551, 109);
            world[13].Build(565, 142);
            world[13].Build(524, 151);
            world[13].Build(526, 117);

            // Iceland
            world[14].Build(493, 80);
            world[14].Build(521, 82);
            world[14].Build(520, 95);
            world[14].Build(489, 97);

            // Northern Europe
            world[15].Build(572, 142);
            world[15].Build(598, 127);
            world[15].Build(633, 127);
            world[15].Build(637, 160);
            world[15].Build(621, 161);
            world[15].Build(621, 150);
            world[15].Build(588, 156);

            // Scandinavia
            world[16].Build(579, 99);
            world[16].Build(627, 63);
            world[16].Build(650, 68);
            world[16].Build(657, 99);
            world[16].Build(609, 124);

            // Southern Europe
            world[17].Build(585, 161);
            world[17].Build(619, 154);
            world[17].Build(617, 164);
            world[17].Build(635, 164);
            world[17].Build(639, 158);
            world[17].Build(654, 157);
            world[17].Build(661, 170);
            world[17].Build(656, 184);
            world[17].Build(636, 192);
            world[17].Build(607, 192);
            world[17].Build(586, 178);

            // Ukraine
            world[18].Build(657, 102);
            world[18].Build(651, 68);
            world[18].Build(750, 66);
            world[18].Build(739, 91);
            world[18].Build(765, 141);
            world[18].Build(735, 138);
            world[18].Build(723, 150);
            world[18].Build(737, 188);
            world[18].Build(714, 184);
            world[18].Build(700, 156);
            world[18].Build(664, 165);
            world[18].Build(655, 154);
            world[18].Build(643, 155);
            world[18].Build(638, 123);

            // Western Europe
            world[19].Build(546, 154);
            world[19].Build(569, 145);
            world[19].Build(581, 155);
            world[19].Build(583, 174);
            world[19].Build(549, 206);
            world[19].Build(521, 202);
            world[19].Build(525, 174);
            world[19].Build(547, 174);
            world[19].Build(552, 166);

            // Congo
            world[20].Build(645, 322);
            world[20].Build(665, 351);
            world[20].Build(690, 354);
            world[20].Build(694, 361);
            world[20].Build(676, 372);
            world[20].Build(665, 418);
            world[20].Build(646, 413);
            world[20].Build(643, 397);
            world[20].Build(605, 392);
            world[20].Build(591, 360);
            world[20].Build(624, 360);
            world[20].Build(615, 339);

            // East Africa
            world[21].Build(654, 271);
            world[21].Build(694, 272);
            world[21].Build(732, 324);
            world[21].Build(760, 318);
            world[21].Build(746, 353);
            world[21].Build(720, 379);
            world[21].Build(714, 414);
            world[21].Build(690, 410);
            world[21].Build(676, 390);
            world[21].Build(698, 362);
            world[21].Build(694, 348);
            world[21].Build(668, 345);
            world[21].Build(654, 322);

            // Egypt
            world[22].Build(600, 220);
            world[22].Build(681, 229);
            world[22].Build(694, 266);
            world[22].Build(612, 265);
            world[22].Build(596, 258);

            // Madagascar
            world[23].Build(759, 429);
            world[23].Build(747, 483);
            world[23].Build(720, 481);
            world[23].Build(730, 439);
            world[23].Build(744, 422);

            // North Africa
            world[24].Build(596, 206);
            world[24].Build(590, 262);
            world[24].Build(646, 284);
            world[24].Build(642, 317);
            world[24].Build(612, 337);
            world[24].Build(613, 355);
            world[24].Build(519, 347);
            world[24].Build(487, 305);
            world[24].Build(515, 224);
            world[24].Build(541, 211);

            // South Africa
            world[25].Build(606, 396);
            world[25].Build(639, 401);
            world[25].Build(642, 417);
            world[25].Build(703, 448);
            world[25].Build(669, 519);
            world[25].Build(627, 519);
            world[25].Build(602, 443);

            // Afghanistan
            world[26].Build(774, 134);
            world[26].Build(833, 140);
            world[26].Build(854, 163);
            world[26].Build(828, 195);
            world[26].Build(793, 208);
            world[26].Build(763, 195);
            world[26].Build(728, 153);
            world[26].Build(740, 144);
            world[26].Build(775, 148);

            // China
            world[27].Build(868, 157);
            world[27].Build(952, 204);
            world[27].Build(992, 183);
            world[27].Build(1031, 240);
            world[27].Build(1022, 263);
            world[27].Build(954, 267);
            world[27].Build(942, 241);
            world[27].Build(929, 235);
            world[27].Build(915, 241);
            world[27].Build(865, 227);
            world[27].Build(864, 211);
            world[27].Build(843, 202);

            // India
            world[28].Build(793, 213);
            world[28].Build(837, 204);
            world[28].Build(859, 215);
            world[28].Build(858, 231);
            world[28].Build(927, 251);
            world[28].Build(922, 269);
            world[28].Build(902, 269);
            world[28].Build(870, 329);
            world[28].Build(862, 328);
            world[28].Build(844, 272);
            world[28].Build(803, 253);

            // Irkutsk
            world[29].Build(909, 97);
            world[29].Build(919, 111);
            world[29].Build(932, 112);
            world[29].Build(938, 106);
            world[29].Build(949, 110);
            world[29].Build(960, 121);
            world[29].Build(1007, 127);
            world[29].Build(1005, 133);
            world[29].Build(1017, 137);
            world[29].Build(1018, 151);
            world[29].Build(997, 135);
            world[29].Build(976, 133);
            world[29].Build(976, 147);
            world[29].Build(925, 146);
            world[29].Build(906, 137);
            world[29].Build(886, 143);
            world[29].Build(908, 112);

            // Japan
            world[30].Build(1075, 168);
            world[30].Build(1096, 177);
            world[30].Build(1097, 215);
            world[30].Build(1064, 233);
            world[30].Build(1053, 218);
            world[30].Build(1082, 204);

            // Kamchatka
            world[31].Build(1046, 68);
            world[31].Build(1147, 82);
            world[31].Build(1108, 144);
            world[31].Build(1066, 111);
            world[31].Build(1058, 179);
            world[31].Build(1046, 173);
            world[31].Build(1043, 153);
            world[31].Build(1031, 156);
            world[31].Build(1019, 144);
            world[31].Build(1024, 137);
            world[31].Build(1019, 132);
            world[31].Build(1009, 131);
            world[31].Build(1000, 115);

            // Middle East
            world[32].Build(661, 187);
            world[32].Build(712, 186);
            world[32].Build(756, 208);
            world[32].Build(771, 199);
            world[32].Build(790, 210);
            world[32].Build(799, 254);
            world[32].Build(792, 272);
            world[32].Build(775, 290);
            world[32].Build(733, 310);
            world[32].Build(691, 232);
            world[32].Build(693, 208);
            world[32].Build(658, 207);

            // Mongolia
            world[33].Build(879, 148);
            world[33].Build(979, 149);
            world[33].Build(979, 136);
            world[33].Build(995, 136);
            world[33].Build(1011, 152);
            world[33].Build(1039, 159);
            world[33].Build(1040, 200);
            world[33].Build(991, 180);
            world[33].Build(954, 199);
            world[33].Build(878, 156);

            // Siam
            // EVERYONE IS HERE!
            world[34].Build(940, 248);
            world[34].Build(950, 274);
            world[34].Build(962, 275);
            world[34].Build(972, 268);
            world[34].Build(985, 274);
            world[34].Build(993, 313);
            world[34].Build(974, 360);
            world[34].Build(949, 331);
            world[34].Build(938, 301);
            world[34].Build(926, 276);

            // Siberia
            world[35].Build(794, 55);
            world[35].Build(871, 44);
            world[35].Build(898, 94);
            world[35].Build(864, 152);
            world[35].Build(858, 108);
            world[35].Build(840, 103);
            world[35].Build(833, 89);

            // Ural
            world[36].Build(746, 90);
            world[36].Build(788, 61);
            world[36].Build(849, 120);
            world[36].Build(858, 161);
            world[36].Build(820, 128);
            world[36].Build(797, 124);
            world[36].Build(767, 130);

            // Yakutsk
            world[37].Build(885, 56);
            world[37].Build(1042, 67);
            world[37].Build(991, 121);
            world[37].Build(962, 116);
            world[37].Build(949, 106);
            world[37].Build(920, 101);
            world[37].Build(893, 85);

            // Eastern Australia
            world[38].Build(1061, 481);
            world[38].Build(1076, 426);
            world[38].Build(1104, 422);
            world[38].Build(1103, 438);
            world[38].Build(1117, 447);
            world[38].Build(1132, 417);
            world[38].Build(1141, 436);
            world[38].Build(1161, 481);
            world[38].Build(1158, 500);
            world[38].Build(1123, 536);
            world[38].Build(1093, 537);
            world[38].Build(1108, 484);

            // Indonesia
            world[39].Build(948, 342);
            world[39].Build(985, 383);
            world[39].Build(1042, 285);
            world[39].Build(1073, 337);
            world[39].Build(1047, 416);
            world[39].Build(975, 400);
            world[39].Build(936, 343);

            // New Guinea
            world[40].Build(1095, 363);
            world[40].Build(1170, 396);
            world[40].Build(1162, 419);
            world[40].Build(1140, 403);
            world[40].Build(1133, 412);
            world[40].Build(1115, 405);
            world[40].Build(1090, 389);

            // Western Australia
            world[41].Build(1073, 434);
            world[41].Build(1060, 481);
            world[41].Build(1107, 486);
            world[41].Build(1092, 536);
            world[41].Build(1064, 508);
            world[41].Build(1004, 522);
            world[41].Build(1006, 468);
            world[41].Build(1041, 454);
            world[41].Build(1065, 428);
            #endregion

            #region connections
            Territory.Connect(world[(int)Ter.Alaska],
                world[(int)Ter.NorthwestTerritory]);
            Territory.Connect(world[(int)Ter.Alaska],
                world[(int)Ter.Alberta]);
            Territory.Connect(world[(int)Ter.Alaska],
                world[(int)Ter.Kamchatka]);
            Territory.Connect(world[(int)Ter.Alberta],
                world[(int)Ter.NorthwestTerritory]);
            Territory.Connect(world[(int)Ter.Alberta],
                world[(int)Ter.Ontario]);
            Territory.Connect(world[(int)Ter.Alberta],
                world[(int)Ter.WesternUnitedStates]);
            Territory.Connect(world[(int)Ter.CentralAmerica],
                world[(int)Ter.EasternUnitedStates]);
            Territory.Connect(world[(int)Ter.CentralAmerica],
                world[(int)Ter.WesternUnitedStates]);
            Territory.Connect(world[(int)Ter.CentralAmerica],
                world[(int)Ter.Venezuela]);
            Territory.Connect(world[(int)Ter.EasternUnitedStates],
                world[(int)Ter.Ontario]);
            Territory.Connect(world[(int)Ter.EasternUnitedStates],
                world[(int)Ter.Quebec]);
            Territory.Connect(world[(int)Ter.EasternUnitedStates],
                world[(int)Ter.WesternUnitedStates]);
            Territory.Connect(world[(int)Ter.Greenland],
                world[(int)Ter.NorthwestTerritory]);
            Territory.Connect(world[(int)Ter.Greenland],
                world[(int)Ter.Ontario]);
            Territory.Connect(world[(int)Ter.Greenland],
                world[(int)Ter.Quebec]);
            Territory.Connect(world[(int)Ter.Greenland],
                world[(int)Ter.Iceland]);
            Territory.Connect(world[(int)Ter.NorthwestTerritory],
                world[(int)Ter.Ontario]);
            Territory.Connect(world[(int)Ter.Ontario],
                world[(int)Ter.Quebec]);
            Territory.Connect(world[(int)Ter.Ontario],
                world[(int)Ter.WesternUnitedStates]);
            Territory.Connect(world[(int)Ter.Argentina],
                world[(int)Ter.Brazil]);
            Territory.Connect(world[(int)Ter.Argentina],
                world[(int)Ter.Peru]);
            Territory.Connect(world[(int)Ter.Brazil],
                world[(int)Ter.Peru]);
            Territory.Connect(world[(int)Ter.Brazil],
                world[(int)Ter.Venezuela]);
            Territory.Connect(world[(int)Ter.Brazil],
                world[(int)Ter.NorthAfrica]);
            Territory.Connect(world[(int)Ter.Peru],
                world[(int)Ter.Venezuela]);
            Territory.Connect(world[(int)Ter.GreatBritain],
                world[(int)Ter.Iceland]);
            Territory.Connect(world[(int)Ter.GreatBritain],
                world[(int)Ter.NorthernEurope]);
            Territory.Connect(world[(int)Ter.GreatBritain],
                world[(int)Ter.Scandinavia]);
            Territory.Connect(world[(int)Ter.GreatBritain],
                world[(int)Ter.WesternEurope]);
            Territory.Connect(world[(int)Ter.Iceland],
                world[(int)Ter.Scandinavia]);
            Territory.Connect(world[(int)Ter.NorthernEurope],
                world[(int)Ter.Scandinavia]);
            Territory.Connect(world[(int)Ter.NorthernEurope],
                world[(int)Ter.SouthernEurope]);
            Territory.Connect(world[(int)Ter.NorthernEurope],
                world[(int)Ter.Ukraine]);
            Territory.Connect(world[(int)Ter.NorthernEurope],
                world[(int)Ter.WesternEurope]);
            Territory.Connect(world[(int)Ter.Scandinavia],
                world[(int)Ter.Ukraine]);
            Territory.Connect(world[(int)Ter.SouthernEurope],
                world[(int)Ter.Ukraine]);
            Territory.Connect(world[(int)Ter.SouthernEurope],
                world[(int)Ter.WesternEurope]);
            Territory.Connect(world[(int)Ter.SouthernEurope],
                world[(int)Ter.Egypt]);
            Territory.Connect(world[(int)Ter.SouthernEurope],
                world[(int)Ter.NorthAfrica]);
            Territory.Connect(world[(int)Ter.SouthernEurope],
                world[(int)Ter.MiddleEast]);
            Territory.Connect(world[(int)Ter.Ukraine],
                world[(int)Ter.Afghanistan]);
            Territory.Connect(world[(int)Ter.Ukraine],
                world[(int)Ter.MiddleEast]);
            Territory.Connect(world[(int)Ter.Ukraine],
                world[(int)Ter.Ural]);
            Territory.Connect(world[(int)Ter.WesternEurope],
                world[(int)Ter.NorthAfrica]);
            Territory.Connect(world[(int)Ter.Congo],
                world[(int)Ter.EastAfrica]);
            Territory.Connect(world[(int)Ter.Congo],
                world[(int)Ter.NorthAfrica]);
            Territory.Connect(world[(int)Ter.Congo],
                world[(int)Ter.SouthAfrica]);
            Territory.Connect(world[(int)Ter.EastAfrica],
                world[(int)Ter.Egypt]);
            Territory.Connect(world[(int)Ter.EastAfrica],
                world[(int)Ter.Madagascar]);
            Territory.Connect(world[(int)Ter.EastAfrica],
                world[(int)Ter.NorthAfrica]);
            Territory.Connect(world[(int)Ter.EastAfrica],
                world[(int)Ter.SouthAfrica]);
            Territory.Connect(world[(int)Ter.EastAfrica],
                world[(int)Ter.MiddleEast]);
            Territory.Connect(world[(int)Ter.Egypt],
                world[(int)Ter.NorthAfrica]);
            Territory.Connect(world[(int)Ter.Egypt],
                world[(int)Ter.MiddleEast]);
            Territory.Connect(world[(int)Ter.Madagascar],
                world[(int)Ter.SouthAfrica]);
            Territory.Connect(world[(int)Ter.Afghanistan],
                world[(int)Ter.China]);
            Territory.Connect(world[(int)Ter.Afghanistan],
                world[(int)Ter.India]);
            Territory.Connect(world[(int)Ter.Afghanistan],
                world[(int)Ter.MiddleEast]);
            Territory.Connect(world[(int)Ter.Afghanistan],
                world[(int)Ter.Ural]);
            Territory.Connect(world[(int)Ter.China],
                world[(int)Ter.India]);
            Territory.Connect(world[(int)Ter.China],
                world[(int)Ter.Mongolia]);
            Territory.Connect(world[(int)Ter.China],
                world[(int)Ter.Siam]);
            Territory.Connect(world[(int)Ter.China],
                world[(int)Ter.Siberia]);
            Territory.Connect(world[(int)Ter.China],
                world[(int)Ter.Ural]);
            Territory.Connect(world[(int)Ter.India],
                world[(int)Ter.MiddleEast]);
            Territory.Connect(world[(int)Ter.India],
                world[(int)Ter.Siam]);
            Territory.Connect(world[(int)Ter.Irkutsk],
                world[(int)Ter.Kamchatka]);
            Territory.Connect(world[(int)Ter.Irkutsk],
                world[(int)Ter.Mongolia]);
            Territory.Connect(world[(int)Ter.Irkutsk],
                world[(int)Ter.Siberia]);
            Territory.Connect(world[(int)Ter.Irkutsk],
                world[(int)Ter.Yakutsk]);
            Territory.Connect(world[(int)Ter.Japan],
                world[(int)Ter.Kamchatka]);
            Territory.Connect(world[(int)Ter.Japan],
                world[(int)Ter.Mongolia]);
            Territory.Connect(world[(int)Ter.Kamchatka],
                world[(int)Ter.Mongolia]);
            Territory.Connect(world[(int)Ter.Kamchatka],
                world[(int)Ter.Yakutsk]);
            Territory.Connect(world[(int)Ter.Mongolia],
                world[(int)Ter.Siberia]);
            Territory.Connect(world[(int)Ter.Siam],
                world[(int)Ter.Indonesia]);
            Territory.Connect(world[(int)Ter.Siberia],
                world[(int)Ter.Ural]);
            Territory.Connect(world[(int)Ter.Siberia],
                world[(int)Ter.Yakutsk]);
            Territory.Connect(world[(int)Ter.EasternAustralia],
                world[(int)Ter.NewGuinea]);
            Territory.Connect(world[(int)Ter.EasternAustralia],
                world[(int)Ter.WesternAustralia]);
            Territory.Connect(world[(int)Ter.Indonesia],
                world[(int)Ter.NewGuinea]);
            Territory.Connect(world[(int)Ter.Indonesia],
                world[(int)Ter.WesternAustralia]);
            Territory.Connect(world[(int)Ter.NewGuinea],
                world[(int)Ter.WesternAustralia]);
            #endregion

            #region thisIsStupid
            terArray = new string[42];
            terArray[0] = "Alaska";
            terArray[1] = "Alberta";
            terArray[2] = "CentralAmerica";
            terArray[3] = "EasternUnitedStates";
            terArray[4] = "Greenland";
            terArray[5] = "NorthwestTerritory";
            terArray[6] = "Ontario";
            terArray[7] = "Quebec";
            terArray[8] = "WesternUnitedStates";
            terArray[9] = "Argentina";
            terArray[10] = "Brazil";
            terArray[11] = "Peru";
            terArray[12] = "Venezuela";
            terArray[13] = "GreatBritain";
            terArray[14] = "Iceland";
            terArray[15] = "NorthernEurope";
            terArray[16] = "Scandinavia";
            terArray[17] = "SouthernEurope";
            terArray[18] = "Ukraine";
            terArray[19] = "WesternEurope";
            terArray[20] = "Congo";
            terArray[21] = "EastAfrica";
            terArray[22] = "Egypt";
            terArray[23] = "Madagascar";
            terArray[24] = "NorthAfrica";
            terArray[25] = "SouthAfrica";
            terArray[26] = "Afghanistan";
            terArray[27] = "China";
            terArray[28] = "India";
            terArray[29] = "Irkutsk";
            terArray[30] = "Japan";
            terArray[31] = "Kamchatka";
            terArray[32] = "MiddleEast";
            terArray[33] = "Mongolia";
            terArray[34] = "Siam";
            terArray[35] = "Siberia";
            terArray[36] = "Ural";
            terArray[37] = "Yakutsk";
            terArray[38] = "EasternAustralia";
            terArray[39] = "Indonesia";
            terArray[40] = "NewGuinea";
            terArray[41] = "WesternAustralia";
            #endregion

            #region moreNameArraysYay
            playerNames = new string[6];
            playerNames[0] = "Red";
            playerNames[1] = "Green";
            playerNames[2] = "Blue";
            playerNames[3] = "Yellow";
            playerNames[4] = "Purple";
            playerNames[5] = "Orange";
            playerColors = new Color[6];
            playerColors[0] = Color.FromArgb(255, 10, 50);
            playerColors[1] = Color.Lime;
            playerColors[2] = Color.DeepSkyBlue;
            playerColors[3] = Color.Yellow;
            playerColors[4] = Color.Violet;
            playerColors[5] = Color.Orange;
            highlightColors = new Color[6];
            highlightColors[0] = Color.Pink;
            highlightColors[1] = Color.LightGreen;
            highlightColors[2] = Color.LightSkyBlue;
            highlightColors[3] = Color.PaleGoldenrod;
            highlightColors[4] = Color.FromArgb(245, 186, 245);
            highlightColors[5] = Color.FromArgb(255, 194, 83);
            continents = new string[6];
            continents[0] = "North America"; // 5
            continents[1] = "South America"; // 2
            continents[2] = "Europe"; // 5
            continents[3] = "Africa"; // 3
            continents[4] = "Asia"; // 7
            continents[5] = "Australia"; // 2
            #endregion

            #region troopCounters
            troopCounters.Add(Alaska);
            troopCounters.Add(Alberta);
            troopCounters.Add(CentralAmerica);
            troopCounters.Add(EasternUnitedStates);
            troopCounters.Add(Greenland);
            troopCounters.Add(NorthwestTerritory);
            troopCounters.Add(Ontario);
            troopCounters.Add(Quebec);
            troopCounters.Add(WesternUnitedStates);
            troopCounters.Add(Argentina);
            troopCounters.Add(Brazil);
            troopCounters.Add(Peru);
            troopCounters.Add(Venezuela);
            troopCounters.Add(GreatBritain);
            troopCounters.Add(Iceland);
            troopCounters.Add(NorthernEurope);
            troopCounters.Add(Scandinavia);
            troopCounters.Add(SouthernEurope);
            troopCounters.Add(Ukraine);
            troopCounters.Add(WesternEurope);
            troopCounters.Add(Congo);
            troopCounters.Add(EastAfrica);
            troopCounters.Add(Egypt);
            troopCounters.Add(Madagascar);
            troopCounters.Add(NorthAfrica);
            troopCounters.Add(SouthAfrica);
            troopCounters.Add(Afghanistan);
            troopCounters.Add(China);
            troopCounters.Add(India);
            troopCounters.Add(Irkutsk);
            troopCounters.Add(Japan);
            troopCounters.Add(Kamchatka);
            troopCounters.Add(MiddleEast);
            troopCounters.Add(Mongolia);
            troopCounters.Add(Siam);
            troopCounters.Add(Siberia);
            troopCounters.Add(Ural);
            troopCounters.Add(Yakutsk);
            troopCounters.Add(EasternAustralia);
            troopCounters.Add(Indonesia);
            troopCounters.Add(NewGuinea);
            troopCounters.Add(WesternAustralia);
            #endregion

            selection = -1;
            // ask how many players are playing
            // store the value
            for (int i = 0; i < 42; i++)
            {
                world[i].Bind(troopCounters[i]);
                world[i].text.BackColor = Color.White;
                world[i].text.Enabled = true;
            }
            Hide(selectedTerritory);
            Hide(ManualButton);
            Hide(AutoButton);
            Hide(AttackButton);
            Hide(BlitzButton);
            Hide(ResetAttButton);
            Hide(FortifyInput);
            Hide(WinButton);
            Hide(FortOne);
            Hide(FortHalf);
            Hide(FortMax);
            Hide(FortConfButton);
            Hide(TroopDeployer);
            Show(PromptBox);
            Show(Confirm);
            Show(playerNumInput);

            status = Status.NumPlayers;
        }

        void ResetGame()
        {
            for (int i = 0; i < world.Count; i++)
            {
                world[i].armies = 0;
                world[i].player = "Neutral";
                world[i].text.BackColor = Color.White;
                world[i].text.Text = "0";
            }

            armiesPlaced = 0;
            armiesEach = 20;
            if (players == 5)
                armiesEach = 25;
            if (players == 4)
                armiesEach = 30;
            if (players == 3)
                armiesEach = 35;
            if (players == 2)
                armiesEach = 50;

            Hide(selectedTerritory);
            Hide(ManualButton);
            Hide(AutoButton);
            Hide(AttackButton);
            Hide(BlitzButton);
            Hide(ResetAttButton);
            Hide(FortifyInput);
            Hide(WinButton);
            Hide(FortOne);
            Hide(FortHalf);
            Hide(FortMax);
            Hide(FortConfButton);
            Hide(TroopDeployer);
            Hide(PromptBox2);
            selectedTerritory.Text = " ";
            Show(PromptBox);
            Show(Confirm);
            Show(playerNumInput);

            PromptBox.Location = new System.Drawing.Point(427, 250);
            PromptBox.Text = "How many players?";

            status = Status.NumPlayers;

            turn = 0;
        }

        private void Confirm_Click(object sender, EventArgs e)
        {
            // if asking how many players?
            // (I might reuse this button, maybe a lot)
            click.Play();
            players = (int)playerNumInput.Value;
            Hide(Confirm);
            Hide(playerNumInput);
            //Hide(PromptBox);
            Show(ManualButton);
            Show(AutoButton);
            PromptBox.Text = "Automatically assign territory?";
            PromptBox.Location = new System.Drawing.Point(365, 535);
            status = Status.WhichClaim;
            dummy.Focus();
        }

        private void AutoButton_Click(object sender, EventArgs e)
        {
            click.Play();
            if (status == Status.WhichClaim)
            {
                status = Status.AutoClaim;
                // auto assign territory
                int playerID = 0;
                bool available;
                int selection = 0;
                for (int i = 0; i < world.Count; i++)
                {
                    //Console.Write(playerNames[playerID] + " gets ");
                    available = false;
                    while (!available)
                    {
                        selection = rand.Next(world.Count);
                        available = (world[selection].player == "Neutral");
                    }
                    //Console.WriteLine(world[selection].name);
                    world[selection].Conquer(playerNames[playerID], 1, 
                        playerColors[playerID]);
                    playerID++;
                    playerID %= players;
                }
                status = Status.WhichStartArmy;
                PromptBox.Text = "Automatically place armies?";
                Show(ManualButton);
                Show(AutoButton);
                return;
            }

            if (status == Status.WhichStartArmy)
            {
                status = Status.AutoStartArmy;
                bool flag;
                selection = 0;
                
                for (int i = 0; i < armiesEach; i++)
                {
                    for (int j = 0; j < players; j++)
                    {
                        flag = false;
                        while (!flag)
                        {
                            selection = rand.Next(world.Count);
                            flag = (world[selection].player == playerNames[j]);
                        }
                        world[selection].Draft(1);
                    }
                }
                status = Status.DraftPhase;
                Hide(AutoButton);
                Hide(ManualButton);
                PromptBox.Text = "It is the " + playerNames[turn] + " player's turn";
                Show(PromptBox2);
                armiesEach = DraftableArmies();
                PromptBox2.Text = armiesEach + " armies left";
                Show(selectedTerritory);
            }
        }

        private void ManualButton_Click(object sender, EventArgs e)
        {
            click.Play();
            if (status == Status.WhichClaim)
            {
                status = Status.ManualClaim;
                Show(PromptBox);
                Show(selectedTerritory);
                Hide(ManualButton);
                Hide(AutoButton);
                PromptBox.Text = "It is the red player's turn.";
                PromptBox.Enabled = false;
            }

            if (status == Status.WhichStartArmy)
            {
                status = Status.ManualStartArmy;
                Show(PromptBox); // this enables the prompt box, which
                Show(selectedTerritory); // makes the cursor come back
                Hide(ManualButton);
                Hide(AutoButton);
                PromptBox.Text = "It is the red player's turn.";
                PromptBox.Enabled = false;
            }
        }

        private int DraftableArmies()
        {
            bool flag;
            int controlledTerritories = 0;
            for (int i = 0; i < world.Count; i++)
            {
                if (world[i].player == playerNames[turn])
                    controlledTerritories++;
            }
            int armiesR = 3;
            int bonusArmies = 0;
            // you get 1 army per territory, with a minimum of 3
            // that 8 could be anything from 8 to 11
            if (controlledTerritories > 8)
            {
                armiesR = controlledTerritories / 3;
            }

            // add continent bonuses
            for (int i = 0; i < continents.Length; i++)
            {
                flag = true;
                for (int j = 0; j < world.Count; j++)
                {
                    if (world[j].continent == continents[i] &&
                        world[j].player != playerNames[turn])
                        flag = false;
                }
                if (flag)
                {
                    switch (i)
                    {
                        case 0:
                            bonusArmies += 5;
                            break;
                        case 1:
                            bonusArmies += 2;
                            break;
                        case 2:
                            bonusArmies += 5;
                            break;
                        case 3:
                            bonusArmies += 3;
                            break;
                        case 4:
                            bonusArmies += 7;
                            break;
                        case 5:
                            bonusArmies += 2;
                            break;
                        default:
                            break;
                    }
                }
            }
            return armiesR + bonusArmies;
        }

        #region forgettableMethods
        private void ResetButton_Click(object sender, EventArgs e)
        {
            click.Play();
            ResetGame();
        }

        private void WinButton_Click(object sender, EventArgs e)
        {
            click.Play();
            ResetGame();
        }

        private void ResetAttButton_Click(object sender, EventArgs e)
        {
            click.Play();
            ResetAttack();
        }

        private void Hide(Control foo)
        {
            foo.Enabled = false;
            foo.Visible = false;
            foo.SendToBack();
            dummy.Focus();
        }

        private void Show(Control foo)
        {
            foo.Enabled = true;
            foo.Visible = true;
            foo.BringToFront();
            dummy.Focus();
        }

        private void Highlight(int territory)
        {
            TextBox text = world[territory].text;
            for (int i = 0; i < players; i++)
            {
                if (text.BackColor == playerColors[i])
                    text.BackColor = highlightColors[i];
            }
        }

        private void Unlight(int territory)
        {
            if (territory == -1)
                return;
            TextBox text = world[territory].text;
            for (int i = 0; i < players; i++)
            {
                if (text.BackColor == highlightColors[i])
                    text.BackColor = playerColors[i];
            }
        }

        private void myCloseButton_Click(object sender, EventArgs e)
        {
            click.Play();
            Close();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            //var ev = e as MouseEventArgs;
            //int country = findCountry(ev.X, ev.Y);
            //if (country != -1)
            //    selectedTerritory.Text = world[country].name;
            //else selectedTerritory.Text = "Unknown/Ocean";
        }

        private void TroopDeployer_Click(object sender, EventArgs e)
        {
            click.Play();
            world[(int)Ter.Afghanistan].Draft(1);
            dummy.Focus();
        }

        int findCountry(int x, int y)
        {
            Point testMe = new Point(x, y);
            for (int i = 0; i < 42; i++)
            {
                if (world[i].IsPointInside(testMe))
                {
                    return i;
                }
            }
            return -1;
        }

        static int Roll(int[] attackers, int[] defenders)
        {
            int fatalities = Math.Min(attackers.Length, defenders.Length);
            int result = 0;
            for (int i = 0; i < attackers.Length; i++)
            {
                attackers[i] = rand.Next(6) + 1;
            }
            for (int i = 0; i < defenders.Length; i++)
            {
                defenders[i] = rand.Next(6) + 1;
            }
            if (attackers.Max() > defenders.Max())
                result++;
            if (fatalities == 2)
            {
                if (attackers[0] == attackers.Max())
                    attackers[0] = 0;
                else if (attackers[1] == attackers.Max())
                    attackers[1] = 0;
                else attackers[2] = 0;
                if (defenders[0] == defenders.Max())
                    defenders[0] = 0;
                else defenders[1] = 0;

                if (attackers.Max() > defenders.Max())
                    result++;
            }
            return result;
        }

        private void FortOne_Click(object sender, EventArgs e)
        {
            click.Play();
            FortifyInput.Value = 1;
            maxMoveMemory = false;
        }

        private void FortMax_Click(object sender, EventArgs e)
        {
            click.Play();
            FortifyInput.Value = FortifyInput.Maximum;
            maxMoveMemory = true;
        }

        private void FortHalf_Click(object sender, EventArgs e)
        {
            click.Play();
            FortifyInput.Value = (int)(FortifyInput.Maximum + 1) / 2;
            maxMoveMemory = false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            PromptBox.Text = e.KeyCode.ToString();
        }

        private void PromptBox_TextChanged(object sender, EventArgs e)
        {
            dummy.Focus();
        }

        private void PromptBox2_Click(object sender, EventArgs e)
        {
            if (sender == FortifyInput)
                click.Play();
            if (sender == playerNumInput)
                click.Play();
            dummy.Focus();
        }
        #endregion

        private void StopAttButton_Click(object sender, EventArgs e)
        {
            click.Play();
            StopAttacking();
        }

        private void StopAttacking()
        {
            selectedTerritory.Text = " ";
            if (status == Status.AttackPhase)
            {
                Unlight(selectionA);
                Unlight(selectionB);
                status = Status.FortifyPhase;
                FortifyInput.Value = 1;
                StopAttButton.Text = "Skip";
                bool canFortify = false;
                for (int i = 0; i < world.Count; i++)
                {
                    if (world[i].player == playerNames[turn] &&
                        world[i].armies > 1)
                        canFortify = true;
                }
                if (!canFortify)
                {
                    AdvanceTurn();
                    return;
                }
                selectionA = -1;
                selectionB = -1;
                PromptBox2.Text = "Pick where to move troops from";
            }
            else if (status == Status.FortifyPhase)
            {
                AdvanceTurn();
            }
        }

        private void AdvanceTurn()
        {
            maxMoveMemory = false;
            Unlight(selectionA);
            Unlight(selectionB);
            status = Status.DraftPhase;
            bool alive = false;
            while (!alive)
            {
                turn++;
                turn %= players;
                for (int i = 0; i < world.Count; i++)
                {
                    if (world[i].player == playerNames[turn])
                        alive = true;
                }
            }
            StopAttButton.Text = "End Turn";
            Hide(StopAttButton);
            armiesEach = DraftableArmies();
            PromptBox.Text = "It is the " + playerNames[turn]
                + " player's turn";
            PromptBox2.Text = armiesEach + " armies left";
            selectedTerritory.Text = " ";
            selectionA = -1;
            selectionB = -1;
        }

        private void AttackButton_Click(object sender, EventArgs e)
        {
            hit.Play();
            // roll
            int attackers = world[selectionA].armies;
            int defenders = world[selectionB].armies;
            int[] attackingDice = new int[Math.Min(attackers - 1, 3)];
            int[] defendingDice = new int[Math.Min(defenders, 2)];
            int fatalities = Math.Min(attackingDice.Length, defendingDice.Length);
            int result = Roll(attackingDice, defendingDice);
            world[selectionB].Draft(-result);
            world[selectionA].Draft(result - fatalities);

            // if the attacker loses
            if (world[selectionA].armies < 2)
            {
                ResetAttack();
                return;
            }

            // if the defender loses
            if (world[selectionB].armies < 1)
            {
                bool winner = true;
                for (int i = 0; i < world.Count; i++)
                {
                    if (world[i].player != playerNames[turn]
                        && i != selectionB)
                        winner = false;
                }
                if (winner)
                {
                    Show(WinButton);
                    WinButton.Text = playerNames[turn] + " player wins!";
                    WinButton.BackColor = playerColors[turn];
                    world[selectionB].text.BackColor = playerColors[turn];
                    return;
                }
                Show(FortifyInput);
                FortifyInput.Value = 1;
                Show(FortOne);
                Show(FortHalf);
                Show(FortMax);
                Show(FortConfButton);
                Show(PromptBox2);
                Hide(AttackButton);
                Hide(BlitzButton);
                Hide(ResetAttButton);
                FortifyInput.Maximum = world[selectionA].armies - 1;
                if (maxMoveMemory)
                    FortifyInput.Value = FortifyInput.Maximum;
                System.Threading.Thread.Sleep(400);
                die.Play();
                // conquer the losing territory
                //   manifest a thing to pick the number
            }
        }

        private void BlitzButton_Click(object sender, EventArgs e)
        {
            explode.Play();
            selectedTerritory.Text = " ";
            bool flag = true;
            while (flag)
            {
                int attackers = world[selectionA].armies;
                int defenders = world[selectionB].armies;
                int[] attackingDice = new int[Math.Min(attackers - 1, 3)];
                int[] defendingDice = new int[Math.Min(defenders, 2)];
                int fatalities = Math.Min(attackingDice.Length, defendingDice.Length);
                int result = Roll(attackingDice, defendingDice);
                world[selectionB].Draft(-result);
                world[selectionA].Draft(result - fatalities);

                // if the attacker loses
                if (world[selectionA].armies < 2)
                {
                    ResetAttack();
                    flag = false;
                    return;
                }

                // if the defender loses
                if (world[selectionB].armies < 1)
                {
                    bool winner = true;
                    for (int i = 0; i < world.Count; i++)
                    {
                        if (world[i].player != playerNames[turn]
                            && i != selectionB)
                            winner = false;
                    }
                    if (winner)
                    {
                        Show(WinButton);
                        WinButton.Text = playerNames[turn] + " player wins!";
                        WinButton.BackColor = playerColors[turn];
                        world[selectionB].text.BackColor = playerColors[turn];
                        return;
                    }
                    Show(FortifyInput);
                    FortifyInput.Value = 1;
                    if (maxMoveMemory)
                        FortifyInput.Value = FortifyInput.Maximum;
                    Show(FortOne);
                    Show(FortHalf);
                    Show(FortMax);
                    Show(FortConfButton);
                    Show(PromptBox2);
                    PromptBox2.Text = "How many armies will you move?";
                    Hide(AttackButton);
                    Hide(BlitzButton);
                    Hide(ResetAttButton);
                    FortifyInput.Maximum = world[selectionA].armies - 1;
                    flag = false;
                    // conquer the losing territory
                    //   manifest a thing to pick the number
                }
            }

            // auto-roll

            // handle winning or losing
        }

        private void ResetAttack()
        {
            Unlight(selectionA);
            Unlight(selectionB);
            selectionA = -1;
            selectionB = -1;
            Hide(AttackButton);
            Hide(ResetAttButton);
            Hide(BlitzButton);
            Show(StopAttButton);
            Show(PromptBox2);
            PromptBox2.Text = "Select where to attack from";
            selectedTerritory.Text = " ";

            bool canStillAttack = false;
            for (int i = 0; i < world.Count; i++)
            {
                if (world[i].player == playerNames[turn] && world[i].armies > 1)
                {
                    for (int j = 0; j < world[i].connections.Count; j++)
                        if (world[i].connections[j].player != playerNames[turn])
                            canStillAttack = true;
                }
            }
            if (!canStillAttack)
            {
                StopAttacking();
            }
        }

        private void FortConfButton_Click(object sender, EventArgs e)
        {
            click.Play();
            Unlight(selectionA);
            Unlight(selectionB);
            Hide(FortConfButton);
            Hide(FortifyInput);
            Hide(FortOne);
            Hide(FortHalf);
            Hide(FortMax);
            selectedTerritory.Text = " ";
            maxMoveMemory = FortifyInput.Value == FortifyInput.Maximum;
            world[selectionA].Draft(-(int)FortifyInput.Value);
            world[selectionB].Draft((int)FortifyInput.Value);
            if (status == Status.AttackPhase)
            {
                //maxMoveMemory = FortifyInput.Value == FortifyInput.Maximum;
                world[selectionB].player = playerNames[turn];
                world[selectionB].text.BackColor = playerColors[turn];
                ResetAttack();
            }
            if (status == Status.FortifyPhase)
                AdvanceTurn();
        }

        private bool Pathfinder(int territoryID, int goalID)
        {
            bool newDiscoveries = true;
            string player = playerNames[turn];
            Territory start = world[territoryID];
            Territory end = world[goalID];

            List<Territory> validDestinations = new List<Territory>();
            validDestinations.Add(start);
            while (newDiscoveries)
            {
                newDiscoveries = false;
                for (int i = 0; i < validDestinations.Count; i++)
                {
                    for (int j = 0; j < validDestinations[i].connections.Count; j++)
                    {
                        if (!validDestinations.Contains(
                            validDestinations[i].connections[j])
                            && validDestinations[i].connections[j].player == player)
                        {
                            if (validDestinations[i].connections[j]
                                == end)
                                return true;
                            newDiscoveries = true;

                            validDestinations.Add(
                                validDestinations[i].connections[j]);
                        }
                    }
                }
            }
            return false;
        }

        private void dummy_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == password[passwordProgress])
                passwordProgress++;
            else passwordProgress = 0;
            if (passwordProgress == 11)
            {
                Show(TroopDeployer);
                passwordProgress = 0;
            }
            else Hide(TroopDeployer);
        }
    }
}
