//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Lao Tszy (lao_tszy@yahoo.co.uk)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Drawing;

namespace fCraft
{
    public class MathCommands
    {
        public const int MaxCalculationExceptions = 100;

        public static void Init()
        {
            CommandManager.RegisterCommand(CdFunc);
            CommandManager.RegisterCommand(CdFuncSurf);
            CommandManager.RegisterCommand(CdFuncFill);
            CommandManager.RegisterCommand(CdIneq);
            CommandManager.RegisterCommand(CdEq);
            CommandManager.RegisterCommand(CdSetCoord);
            CommandManager.RegisterCommand(CdSetParam);
            CommandManager.RegisterCommand(CdStartParam);
            CommandManager.RegisterCommand(CdClearParam);
            CommandManager.RegisterCommand(CdSpring);
            CommandManager.RegisterCommand(CdPolarRose);
        }
        const string commonFuncHelp = "Can also be x=f(y, z) or y=f(x, z). ";

    	private const string commonHelp =
    		"Allowed operators: +, -, *, /, %, ^. Comparison and logical operators: >, <, =, &, |, !." +
    		"Constants: e, pi. " +
    		"Functions: sqrt, sq, exp, lg, ln, log(num, base), abs, sign, sin, cos, tan, sinh, cosh, tanh. Example: 1-exp(-1/sq(x)). " +
    		"'sq' stands for 'square', i.e. sq(x) is x*x. ";
            
        const string commonScalingHelp =
			"Select 2 points to define a volume (same as e.g. for cuboid), where the function will be drawn. " +
            "Coords are whole numbers from 0 to the corresponding cuboid dimension length. " +
            "Using 'u' as a scaling switch coords to be [0, 1] along the corresponding cuboid axis. " +
            "'uu' switches coords to be [-1, 1] along the corresponding cuboid axis.";
        const string copyright = "\n(C) 2012 Lao Tszy";


        static readonly CommandDescriptor CdFunc = new CommandDescriptor
            {
                Name = "Func",
                Aliases = new string[] { "fu" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Draws a 3d function values grid, i.e. this variant doesn't care to fill gaps between too distinct values. " +
                    commonFuncHelp + commonHelp + commonScalingHelp + copyright,
                Usage = "/fu z=<f(x, y) expression> [scaling]",
                Handler = FuncHandler,
            };
        
        static readonly CommandDescriptor CdFuncSurf = new CommandDescriptor
            {
                Name = "FuncSurf",
                Aliases = new string[] { "fus" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Draws a 3d function surface. " +
                    commonFuncHelp + commonHelp + commonScalingHelp + copyright,
                Usage = "/fus <f(x, y) expression> [scaling]",
                Handler = FuncSHandler,
            };
        
        static readonly CommandDescriptor CdFuncFill = new CommandDescriptor
            {
                Name = "FuncFill",
                Aliases = new string[] { "fuf" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Draws a 3d function filling the area under it. " +
                    commonFuncHelp + commonHelp + commonScalingHelp + copyright,
                Usage = "/fuf <f(x, y) expression> [scaling]",
                Handler = FuncFHandler,
            };

        //inequality
        static readonly CommandDescriptor CdIneq = new CommandDescriptor
            {
                Name = "Ineq",
                Aliases = new string[] { "ie" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Draws a volume where the specified inequality holds. " +
                    commonHelp + commonScalingHelp + copyright,
                Usage = "/ineq f(x, y, z)>g(x, y, z) [scaling]",
                Handler = InequalityHandler,
            };
        
        //equality
        static readonly CommandDescriptor CdEq = new CommandDescriptor
            {
                Name = "Eq",
                Aliases = new string[] { },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Draws a volume where the specified equality holds. " +
                    commonHelp + commonScalingHelp + copyright,
                Usage = "/eq f(x, y, z)=g(x, y, z) [scaling]",
                Handler = EqualityHandler,
            };
        
        //parametrized manifold
        static readonly CommandDescriptor CdSetCoord = new CommandDescriptor
            {
                Name = "SetCoordParm",
                Aliases = new string[] { "SetCP", "scp" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Sets the parametrization function for a coordinate (x, y, or z). " +
                    "Example: /scp x=2*t+sin(u*v). " +
                    commonHelp + copyright,
                Usage = "/scp <coord variable>=<expression of f(t, u, v)>",
                Handler = PrepareParametrizedManifold.SetParametrization,
            };
        
        static readonly CommandDescriptor CdSetParam = new CommandDescriptor
            {
                Name = "SetParamIter",
                Aliases = new string[] { "SetPI", "spi" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Sets the loop for iteration of the parameter variable (t, u, or v). " +
                    "Example: /spi t 0 3.14 0.314" +
                    copyright,
                Usage = "/spi <param variable> <from> <to> <step>",
                Handler = PrepareParametrizedManifold.SetParamIteration,
            };
        
        static readonly CommandDescriptor CdStartParam = new CommandDescriptor
            {
                Name = "StartParmDraw",
                Aliases = new string[] { "StartPD", "spd" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Usage: /spd [scaling]. Starts drawing the parametrization prepared by commands SetCoordParametrization and SetParamIteration." +
                    commonScalingHelp + copyright,
                Handler = StartParametrizedDraw,
            };
        
        static readonly CommandDescriptor CdClearParam = new CommandDescriptor
            {
                Name = "ClearParmDraw",
                Aliases = new string[] { "ClearPD", "cpd" },
                Category = CommandCategory.Math,
                Permissions = new Permission[] { Permission.DrawAdvanced },
                RepeatableSelection = true,
                Help =
                    "Deletes expressions prepared by commands SetCoordParametrization and SetParamIteration." + copyright,
                Usage = "/cpd",
                Handler = PrepareParametrizedManifold.ClearParametrization,
            };

        static readonly CommandDescriptor CdSpring = new CommandDescriptor
        {
            Name = "Spring",
            Aliases = new[] { "Helix" },
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            Usage = "/Spring [Number of revolutions]",
            Help = "Draws up to 9 revolutions of a spring in the given area.",
            NotRepeatable = true,
            Handler = SpringHandler,
        };
        
        static readonly CommandDescriptor CdPolarRose = new CommandDescriptor
        {
            Name = "PolarRose",
            Aliases = new[] { "pr", "rose" },
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            Usage = "/PolarRose [Number of Petals] [Length of Petals] [Number of Revolutions] [Height Upward]",
            Help = "Draws a polar rose. Leave height blank for flat rose. " +
                   "The lengths and heights expand with your chosen interval, so they are relative. " +
                   "&cRanges: &hPetals[3,infinity), Length[1,4], Revolutions[1,4), Height[1,infinity).",
            NotRepeatable = true,
            Handler = PolarRoseHandler,
        };


        //Those handler functions would be a template function when this <censored> c# could 
        //accept constructors with params for the template param types.
        //One still can use two-fase-construction to enable templetization here,
        //but this seems to me even uglier than copy-pasted handlers
        private static void FuncHandler(Player player, Command cmd)
        {
            FuncDrawOperation operation = new FuncDrawOperationPoints(player, cmd);
            DrawOperationBegin(player, cmd, operation);
        }
        private static void FuncSHandler(Player player, Command cmd)
        {
            FuncDrawOperation operation = new FuncDrawOperationSurface(player, cmd);
            DrawOperationBegin(player, cmd, operation);
        }
        private static void FuncFHandler(Player player, Command cmd)
        {
            try
            {
                FuncDrawOperation operation = new FuncDrawOperationFill(player, cmd);
                DrawOperationBegin(player, cmd, operation);
            }
            catch (Exception e)
            {
                player.Message("Error: " + e.Message);
            }
        }
        private static void InequalityHandler(Player player, Command cmd)
        {
            try
            {
                InequalityDrawOperation operation = new InequalityDrawOperation(player, cmd);
                DrawOperationBegin(player, cmd, operation);
            }
            catch (Exception e)
            {
                player.Message("Error: " + e.Message);
            }
        }
        private static void EqualityHandler(Player player, Command cmd)
        {
            try
            {
                EqualityDrawOperation operation = new EqualityDrawOperation(player, cmd);
                DrawOperationBegin(player, cmd, operation);
            }
            catch (Exception e)
            {
                player.Message("Error: " + e.Message);
            }
        }
        private static void StartParametrizedDraw(Player player, Command cmd)
        {
            try
            {
                ManifoldDrawOperation operation = new ManifoldDrawOperation(player, cmd);
                DrawOperationBegin(player, cmd, operation);
            }
            catch (Exception e)
            {
                player.Message("Error: " + e.Message);
            }
        }
        private static void StartCmdDraw(Player player, Command cmd) //for future use with math cmds such as /Spring and /PolarRose
        {
            try
            {
                StartSpringDraw operation = new StartSpringDraw(player, cmd);
                DrawOperationBegin(player, cmd, operation);
            }
            catch (Exception e)
            {
                player.Message("Error: " + e.Message);
            }
        }


        //copy-paste from BuildingCommands
        private static void DrawOperationBegin(Player player, Command cmd, DrawOperation op)
        {
            IBrushInstance instance = player.Brush.MakeInstance(player, cmd, op);
            if (instance != null)
            {
                op.Brush = instance;
                player.SelectionStart(op.ExpectedMarks, new SelectionCallback(DrawOperationCallback), op, new Permission[] { Permission.DrawAdvanced });
                player.Message("{0}: Click {1} blocks or use &H/Mark&S to make a selection.", new object[] { op.Description, op.ExpectedMarks });
            }
        }
        private static void DrawOperationCallback(Player player, Vector3I[] marks, object tag)
        {
            DrawOperation operation = (DrawOperation)tag;
            if (operation.Prepare(marks))
            {
                if (!player.CanDraw(operation.BlocksTotalEstimate))
                {
                    player.Message("You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.", new object[] { player.Info.Rank.DrawLimit, operation.Bounds.Volume });
                    operation.Cancel();
                }
                else
                {
                    player.Message("{0}: Processing ~{1} blocks.", new object[] { operation.Description, operation.BlocksTotalEstimate });
                    operation.Begin();
                }
            }
        }
         private static void SpringHandler(Player player, Command cmd)       //for /spring
        {
            string revolutions = cmd.Next();
            int rev;
            bool parsed = Int32.TryParse(revolutions, out rev); //tries to convert input to int

            if (player.Can(Permission.DrawAdvanced))
            {

                PrepareSpring.SetParametrization(player, new Command("/scp x=(1+0.2*cos(2*pi*v))*sin(2*pi*u)"));
                PrepareSpring.SetParametrization(player, new Command("/scp y=(1+0.2*cos(2*pi*v))*cos(2*pi*u)"));
                PrepareSpring.SetParametrization(player, new Command("/scp z=u+0.2*sin(2*pi*v)"));

                if (revolutions == null || rev == 1)     //if number of revolutions isnt specified, just does 1
                {
                    PrepareSpring.SetParamIteration(player, new Command("/spi u 0 1 0.005"));
                    PrepareSpring.SetParamIteration(player, new Command("/spi v 0 1 0.005"));
                }

                else if (rev > 9 || rev <= 0)        //The greatest number of revolutions without having to adjust the number of iteration steps. I would adjust the number
                {                                    // of iterations per requested number of revolutions, but it would make the spring look like the blocks were placed too sparingly.
                    player.Message("The number of revolutions must be between 1 and 9.");
                    return;
                }

                else if (rev > 1 && rev < 5)         //different amount of iteration steps when different number of revolutions. Makes the springs look more filled in.
                {
                    PrepareSpring.SetParamIteration(player, new Command("/spi u 0 " + rev + " 0.005"));
                    PrepareSpring.SetParamIteration(player, new Command("/spi v 0 " + rev + " 0.005"));
                }

                else if (rev <= 9 && rev >= 5)       //different amount of iteration steps when different number of revolutions. Makes the springs look more filled in.
                {
                    PrepareSpring.SetParamIteration(player, new Command("/spi u 0 " + rev + " 0.0099"));
                    PrepareSpring.SetParamIteration(player, new Command("/spi v 0 " + rev + " 0.0099"));
                }
                StartCmdDraw(player, new Command("/spd uu"));       //uses custom handler as to not display messages to the user
            }
            else
            {
                CdSpring.PrintUsage(player);
            }
        }
        private static void PolarRoseHandler(Player player, Command cmd)
        {
            //prepping variables, converting them to ints to enter into the equations
            string petals = cmd.Next();
            int pet;
            bool parsedPet = Int32.TryParse(petals, out pet);
            int petTest = (pet / 2);

            string length = cmd.Next();
            int len;
            bool parsedLen = Int32.TryParse(length, out len);

            string revolutions = cmd.Next();
            int rev;
            bool parsedRev = Int32.TryParse(revolutions, out rev);
            double numRev = (6.28 * rev);

            string Height = cmd.Next();
            int height;
            bool parsedHeight = Int32.TryParse(Height, out height);

            double NumHeight = (height * 0.01);             //This makes the Height more managable for the user
            double RevIter = (0.01 + (rev * 0.005));        //Iteration needs to be adjusted based on how many revolutions are made.
            double RevIter6 = (rev * 0.015);                //Seperate RevIter for when 6 petals (required because of method used for 6 petals)

            if (player.Can(Permission.DrawAdvanced) && pet > 2 && len > 0 && len < 5 && rev > 0 && rev < 5)
            {
                if (!parsedPet || !parsedLen || !parsedRev)                //if the player enters in invalid values for length or number of petals
                {
                    player.Message("Please enter whole number values for each of the variables. (Height is optional) Type /help pr for the ranges.");
                    return;
                }

                if (petals == null || length == null || revolutions == null)
                {
                    player.Message("Please enter values for number of petals, length of petals and number of revolutions.");
                }

                if (pet == 4 || pet == 8 || pet == 10 || pet == 12 || pet == 14 || pet == 16)   //When the number of petals is even, the  number entered has to be halved to get the
                {                                                                               //right number of petals. I figured numbers over 16, the user wouldn't even notice.
                    PrepareSpring.SetParametrization(player, new Command("/scp x=" + len + "*sin(" + petTest + "*u)*cos(u)"));
                    PrepareSpring.SetParametrization(player, new Command("/scp y=" + len + "*sin(" + petTest + "*u)*sin(u)"));
                }

                else if (pet == 6)       //The math behind this is complicated, but getting 6 petals with this method is impossible. I am using a different method 
                {                   //for when the user asks for 6 petals. They will be slightly overlapping, unlike the others.
                    PrepareSpring.SetParametrization(player, new Command("/scp x=" + len + "*sin(1.5*u)*cos(u)"));
                    PrepareSpring.SetParametrization(player, new Command("/scp y=" + len + "*sin(1.5*u)*sin(u)"));
                }

                else
                {
                    PrepareSpring.SetParametrization(player, new Command("/scp x=" + len + "*sin(" + pet + "*u)*cos(u)"));
                    PrepareSpring.SetParametrization(player, new Command("/scp y=" + len + "*sin(" + pet + "*u)*sin(u)"));
                }

                if (Height == null || Height.Length == 0) //If no height is specified, the rose will be flat.
                {
                    PrepareSpring.SetParametrization(player, new Command("/scp z=0"));
                }

                else if (Height.Length >= 1 && parsedHeight)
                {
                    PrepareSpring.SetParametrization(player, new Command("/scp z=" + NumHeight + "*u"));
                }

                else
                {
                    player.Message("Please enter whole integer values for each of the variables. (Height is optional) Type /help pr for the ranges.");
                    return;
                }

                if (pet == 6)
                {
                    PrepareSpring.SetParamIteration(player, new Command("/spi u 0 12.56 " + RevIter6));
                    PrepareSpring.SetParamIteration(player, new Command("/spi v 0 12.56 " + RevIter6));
                }

                else
                {
                    PrepareSpring.SetParamIteration(player, new Command("/spi u 0 " + numRev + " " + RevIter));
                    PrepareSpring.SetParamIteration(player, new Command("/spi v 0 " + numRev + " " + RevIter));
                }

                StartCmdDraw(player, new Command("/spd uu"));           //uses custom handler as to not display messages to user
            }

            else if (pet < 3 || len < 1 || len > 4 || rev < 0 || height < 1 || rev > 4)
            {
                player.Message("&cRanges: &hPetals[3,infinity), Length[1,4], Revolutions[1,4), Height[1,infinity).");
                return;
            }

            else
            {
                CdPolarRose.PrintUsage(player);
            }
        }
    }
}



