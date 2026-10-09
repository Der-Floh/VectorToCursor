using VectorToCursor.Application;
using VectorToCursor.Cli;
using VectorToCursor.Cursors;
using VectorToCursor.Installation;
using VectorToCursor.Rendering;
using Velopack;

SemanticVersion? installedVersion = null;
VelopackApp.Build()
    .AddFolderToUserPath(AppContext.BaseDirectory)
    .OnFirstRun(version => installedVersion = version)
    .Run();

if (installedVersion is not null)
    return FirstRunMessage.Show(installedVersion.ToString());

ICursorConverter converter = new CursorConverter(new SkiaSvgLoader(), new ImageSharpCursorEncoder(), new AniEncoder());
return RootCommandFactory.Create(converter).Parse(args).Invoke();
