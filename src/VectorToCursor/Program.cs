using VectorToCursor.Application;
using VectorToCursor.Cli;
using VectorToCursor.Cursors;
using VectorToCursor.Rendering;

ICursorConverter converter = new CursorConverter(new SkiaSvgLoader(), new ImageSharpCursorEncoder());
return RootCommandFactory.Create(converter).Parse(args).Invoke();
