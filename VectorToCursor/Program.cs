using VectorToCursor.Application;
using VectorToCursor.Cli;
using VectorToCursor.Cursors;
using VectorToCursor.Installation;
using VectorToCursor.Rendering;
using Velopack;

bool launchedBySetup = false;
VelopackApp.Build()
    .AddFolderToUserPath(AppContext.BaseDirectory)
    .OnFirstRun(_ => launchedBySetup = true)
    .Run();

if (launchedBySetup)
    return ExitCodes.Success;

ICursorConverter converter = new CursorConverter(new SkiaSvgLoader(), new ImageSharpCursorEncoder(), new AniEncoder());
return RootCommandFactory.Create(converter).Parse(args).Invoke();
