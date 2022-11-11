using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native;

public abstract class Prototype : ObjectInstance
{
    internal readonly Realm _realm;

    private protected Prototype(Engine engine, Realm realm) : base(engine)
    {
        _realm = realm;
    }
}
