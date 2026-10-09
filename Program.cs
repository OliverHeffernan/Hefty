using Hefty.Engine;
using Hefty.Examples.Worlds;

using var game = new HeftyGame(args.Length > 0 && args[0] == "--menu" ? new MainMenu() : new LevelOne());
game.Run();
