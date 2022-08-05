using MonoMod.Utils;
using Quintessential;
using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using MonoMod.RuntimeDetour;

namespace Omg
{

    public enum FramingMode
    {
        Default,
        Equilibrium,
        Bounds,
    }

    public class Omg : QuintessentialMod
    {
        public string solution_name = "";
        public string file_out = "";
        public int start_cycle = -1;
        public int end_cycle = -1;
        public int frames_per_cycle = 6;
        public int speed = 5;

        public Bounds2 bound = Bounds2.Empty;

        public FramingMode framing = FramingMode.Default;

        public void class250_ctor(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if(
                cursor.TryGotoNext(MoveType.After, instr => instr.Match(OpCodes.Ldarg_1))
            )
            {
                // Fucking anhilate this part of the function
                cursor.RemoveRange(24);
                // Convert Solution to bounds
                cursor.EmitDelegate<Func<Solution, Bounds2>>((Solution solution) => {
                    Bounds2 result = Bounds2.Empty;
                    switch(framing)
                    {
                        case FramingMode.Default:
                            // If it this is a production puzzle, expand by a set amount
                            if (solution.method_1934().field_2779.method_1085())
                            {
                                result = solution.method_1934().field_2779.method_1087().field_2074.Expanded(125f, 125f, 125f, 75f);
                                break;
                            }
                            result = solution.method_1955().Expanded(75f, 75f);
                            break;
                        case FramingMode.Equilibrium:
                            // don't do anything weird for production puzzles
                            if (solution.method_1934().field_2779.method_1085())
                            {
                                result = solution.method_1934().field_2779.method_1087().field_2074.Expanded(125f, 125f, 125f, 75f);
                                break;
                            }
                            // This is a modified version of Solution.method_1955 which only targets Glyphs of Eq. 
                            HashSet<HexIndex> hashSet = new HashSet<HexIndex>();
                            foreach(Part part in solution.method_1937())
                            {     // If part type != Marker
                                if (part.method_1159() != class_191.field_1782)
                                {
                                    continue;
                                }
                                HashSet<HexIndex> other = part.method_1186(solution);
                                hashSet.UnionWith(other);
                            }
                            bool flag = true;
                            foreach (HexIndex item2 in hashSet)
                            {
                                Vector2 vector = class_187.field_1742.method_491(item2, Vector2.Zero);
                                if (flag)
                                {
                                    result= Bounds2.WithCorners(vector, vector);
                                    flag = false;
                                }
                                else
                                {
                                    result = result.UnionedWith(vector);
                                }
                            }

                            // If there's no eq glyphs just use the default
                            if(result.Min.X == 0 && result.Min.Y == 0 && result.Max.X == 0 && result.Max.Y == 0)
                            {
                                result = solution.method_1955().Expanded(75f, 75f);
                            }
                            break;
                        case FramingMode.Bounds:
                            result = bound;
                            break;
                        default:
                            new Exception("Undefined framing mode... How'd you do this?");
                            break;
                    }
                    return result;
                });
                // loc.0 is used later in the method so it has to be stored & loaded
                cursor.Emit(OpCodes.Stloc_0);
                cursor.Emit(OpCodes.Ldloc_0);
                // Uncap zooming for custom framing modes
                cursor.EmitDelegate<Func<Bounds2, float>>((Bounds2 bounds) => {
                    float result = 1.0f;
                        if(framing == FramingMode.Default){
                            result = Math.Max(1f, Math.Max(bounds.Width / 802.0f, bounds.Height / 533.0f));
                        } else {
                            if(bounds.Width > 0 && bounds.Height > 0)
                            {    
                                result = Math.Max(bounds.Width / 802.0f, bounds.Height / 533.0f);
                            } else 
                            {
                                result = Math.Max(1f, Math.Max(bounds.Width / 802.0f, bounds.Height / 533.0f));
                            }
                        }
                    return result;
                });
                
                // Output file stuff
                if(
                    cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall(typeof(Path), "Combine"))
                )  
                {
                    cursor.Remove();
                    cursor.EmitDelegate<Func<string, string, string>>((string text3, string text2) => {
                        if(file_out != "")
                        {
                            return file_out;
                        } 
                        else
                        {
                            return Path.Combine(text3, text2);
                        }
                    });
                } else {
                    throw new Exception("Failed to modify bounds (Couldn't modify output file)");
                }
            } else {
                throw new Exception("Failed to modify bounds (Couldn't find solution loading)");
            }
        }

        // Wacky attempt to speed up loading by throwing some singletons down the drain
        //... didn't work.
        // If someone can figure out how to cut out texture loading properly (my attmpt made Nothing Good Happen)
        //then that would be pog
        public void GameLogicInitSingletons(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.RemoveRange(12);
            cursor.EmitDelegate<Action>(() => {
                if(solution_name != "") {
                    class_134.method_248();
                    class_175.method_248();
                    class_167.method_471();
                    class_191.method_496();
                    class_169.method_476();
                    class_214.method_558();
                    Puzzles.method_1285();
                    class_172.method_480();
                    Campaigns.method_828();
                    JournalVolumes.method_1052();
                    // class_107.method_143();
                }
                else
                {
                    class_134.method_248();
                    Transitions.method_2181();
                    class_175.method_248();
                    class_167.method_471();
                    class_191.method_496();
                    class_169.method_476();
                    class_214.method_558();
                    Puzzles.method_1285();
                    class_172.method_480();
                    Campaigns.method_828();
                    JournalVolumes.method_1052();
                    class_107.method_143();
                }
            });
        }

        public void RemoveMarkOnFrame(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if(
                cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(typeof(class_250), "MarkOnFrame"))
            ) {
                cursor.Remove();
            }
        }

        void class250_method_50(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if(QuintessentialLoader.CodeMods.Count <= 2)
            {
                if(cursor.TryGotoNext(
                    MoveType.Before, 
                    instr => instr.MatchCall(typeof(class_250), "MarkOnFrame")
                ))
                {
                    Logger.Log("Omg: No other mods detected, removing mark.");
                    cursor.Remove();
                }
            }
            if (
                cursor.TryGotoNext(MoveType.Before,
                instr => instr.Match(OpCodes.Ret))
            ) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(class_250).GetField("field_2026", BindingFlags.Instance | BindingFlags.NonPublic));
                
                cursor.EmitDelegate<Action<bool>>((bool field_2026) => {
                    if (!field_2026) return;
                    foreach(string arg in Environment.GetCommandLineArgs())
                    {
                        if(arg.EndsWith(".solution"))
                        {
                            Logger.Log("Omg: Successfully created, closing the game...");
                            GameLogic.field_2434.method_963(0);
                        }
                    }
                });
            }
        }

        public ILHook GameLogic_method_956;

        public override void Load() {
            IL.class_250.ctor += class250_ctor;
            GameLogic_method_956 = new ILHook(typeof(GameLogic).GetMethod("orig_method_956", BindingFlags.Instance | BindingFlags.NonPublic), 
                                            GameLogicInitSingletons);
            
            // Parse command line arguments
            foreach(string arg in Environment.GetCommandLineArgs())
            {
                if(arg.EndsWith(".solution"))
                {
                    solution_name = arg;
                } else if(arg.StartsWith("out=")) {
                    file_out = arg.Split('=')[1];
                } else if(arg.StartsWith("start=")) {
                    if(!int.TryParse(arg.Split('=')[1], out start_cycle)) {
                        Logger.Log("Omg: Error, start cycle not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("end=")) {
                    if(!int.TryParse(arg.Split('=')[1], out end_cycle)) {
                        Logger.Log("Omg: Error, end cycle not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("fpc=")) {
                    if(!int.TryParse(arg.Split('=')[1], out frames_per_cycle)) {
                        Logger.Log("Omg: Error, frames per cycle not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("speed=")) {
                    if(!int.TryParse(arg.Split('=')[1], out speed)) {
                        Logger.Log("Omg: Error, speed not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("framing=")) {
                    switch(arg.Split('=')[1])
                    {
                        case "eq":
                            framing = FramingMode.Equilibrium;
                            break;
                        case "bounds":
                            framing = FramingMode.Bounds;
                            break;
                        default:
                            framing = FramingMode.Default;
                            break;
                    }
                } else if(arg.StartsWith("min=")) {
                    string[] coords = arg.Split('=')[1].Split(',');
                    if(!float.TryParse(coords[0], out bound.Min.X)) {
                        Logger.Log("Omg: Error, speed not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                    if(!float.TryParse(coords[1], out bound.Min.Y)) {
                        Logger.Log("Omg: Error, speed not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith(value: "max=")) {
                    string[] coords = arg.Split('=')[1].Split(',');
                    if(!float.TryParse(coords[0], out bound.Max.X)) {
                        Logger.Log("Omg: Error, speed not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                    if(!float.TryParse(coords[1], out bound.Max.Y)) {
                        Logger.Log("Omg: Error, speed not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                }
            }
            IL.class_250.method_50 += class250_method_50;
        }

        public override void LoadPuzzleContent() {
        }

        public override void PostLoad() {
            if(solution_name != "")
            {
                Maybe<Solution> maybeSolution = Solution.method_1958(solution_name);
                class_162.method_403(maybeSolution.method_1085(), "Failed to load solution file.");
                Solution solution = maybeSolution.method_1087();
                var class250Type = typeof(class_250);
                // Thanks, GuiltyBystander!
                DynData<class_250> class250 = (new DynData<class_250>(null));
                
                if(frames_per_cycle > 0) {
                    class250["field_2017"] = frames_per_cycle;
                }

                if(speed > 0) {
                    class250["field_2018"] = speed;
                }
                
                class_250 gifScreen = new class_250(solution);
                
                if(start_cycle > -1 && end_cycle > start_cycle) {
                    class250Type.GetField("field_2028", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gifScreen, start_cycle);
                    class250Type.GetField("field_2029", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gifScreen, end_cycle);
                    class250Type.GetField("field_2030", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gifScreen, true);
                }
                GameLogic.field_2434.method_946(gifScreen);
            }
        }
        

        public override void Unload() {
            GameLogic_method_956.Dispose();
            IL.class_250.ctor -= class250_ctor;
            IL.class_250.method_50 -= class250_method_50;
        }
    }
}