using Esprima;
using Esprima.Ast;
using Jint.Native;
using Jint.Native.Json;

namespace Jint.Runtime.Modules;

/// <summary>
/// Base template for module loaders.
/// </summary>
public abstract class ModuleLoader : IModuleLoader
{
    public abstract ResolvedSpecifier Resolve(string? referencingModuleLocation, ModuleRequest moduleRequest);

    public ModuleRecord LoadModule(Engine engine, ResolvedSpecifier resolved)
    {
        string code;
        try
        {
            code = LoadModuleContents(engine, resolved);
        }
        catch (Exception)
        {
            ExceptionHelper.ThrowJavaScriptException(engine, $"Could not load module {resolved.ModuleRequest.Specifier}", (Location) default);
            return default!;
        }

        var isJson = resolved.ModuleRequest.Attributes != null
                     && Array.Exists(resolved.ModuleRequest.Attributes, x => string.Equals(x.Key, "type", StringComparison.Ordinal) && string.Equals(x.Value, "json", StringComparison.Ordinal));

        ModuleRecord moduleRecord = isJson
            ? BuildJsonModule(engine, resolved, code)
            : BuildSourceTextModule(engine, resolved, code);

        return moduleRecord;
    }

    protected abstract string LoadModuleContents(Engine engine, ResolvedSpecifier resolved);

    private static SyntheticModuleRecord BuildJsonModule(Engine engine, ResolvedSpecifier resolved, string code)
    {
        var source = resolved.Uri?.LocalPath;
        JsValue module;
        try
        {
            module = new JsonParser(engine).Parse(code);
        }
        catch (Exception)
        {
            ExceptionHelper.ThrowJavaScriptException(engine, $"Could not load module {source}", (Location) default);
            module = null;
        }

        return new SyntheticModuleRecord(engine, engine.Realm, module, resolved.Uri?.LocalPath);
    }
    private static SourceTextModuleRecord BuildSourceTextModule(Engine engine, ResolvedSpecifier resolved, string code)
    {
        var source = resolved.Uri?.LocalPath;
        Module module;
        try
        {
            module = new JavaScriptParser().ParseModule(code, source);
        }
        catch (ParserException ex)
        {
            ExceptionHelper.ThrowSyntaxError(engine.Realm, $"Error while loading module: error in module '{source}': {ex.Error}");
            module = null;
        }
        catch (Exception)
        {
            ExceptionHelper.ThrowJavaScriptException(engine, $"Could not load module {source}", (Location) default);
            module = null;
        }

        return new SourceTextModuleRecord(engine, engine.Realm, module, resolved.Uri?.LocalPath, async: false);
    }
}
