using System.Collections.Generic;
using System.Linq;
using Breakaleg.Core.Models;

namespace Breakaleg.Core.Dynamic
{
    public delegate Instance NativeCodeCallback(Instance self, params Instance[] args);

    public abstract class DynamicRecord
    {
        protected Dictionary<object, Instance> Fields;

        public void ShareFieldsWith(DynamicRecord recTo)
        {
            if (this.Fields == null)
                this.Fields = new Dictionary<object, Instance>();
            recTo.Fields = this.Fields;
        }

        public virtual Instance GetField(object name)
        {
            Instance f;
            if (Fields != null && Fields.TryGetValue(name, out f))
                return f;
            return null;
        }

        public Instance SetField(object name, Instance inst)
        {
            (Fields ?? (Fields = new Dictionary<object, Instance>()))[name] = inst;
            return inst;
        }

        public bool DeleteField(object name)
        {
            return Fields != null ? Fields.Remove(name) : false;
        }

        public Instance SetMethod(object name, NativeCodeCallback nativeCode)
        {
            return SetField(name, new Instance
                                      {
                                          Prototype = new Instance { Code = new FunctionCode(nativeCode) }
                                      });
        }

        public Instance[] GetFields()
        {
            if (Fields == null)
                return null;
            var fieldArray = Fields.ToArray();
            var instArray = new Instance[fieldArray.Length];
            for (var i = 0; i < fieldArray.Length; i++)
                instArray[i] = fieldArray[i].Value;
            return instArray;
        }
    }
}
