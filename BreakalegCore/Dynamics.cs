using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Breakaleg.Core.Models
{
    public abstract class DynamicRecord
    {
        protected Dictionary<object, Instance> members;

        protected virtual object GetValue() { return null; }

        public virtual Instance GetMember(object name)
        {
            if (members != null)
            {
                Instance member;
                members.TryGetValue(name, out member);
                if (member != null)
                    return member;
            }

            var oInst = GetValue();
            if (oInst == null)
                return null;

            var oType = (oInst is Type) ? (Type)oInst : oInst.GetType();
            var self = (oInst is Type) ? null : oInst;
            if (name is string)
            {
                var mInfo = oType.GetMember((string)name).FirstOrDefault();
                if (mInfo == null)
                    return null;
                dynamic mValue = null;
                if (mInfo is PropertyInfo)
                    mValue = (mInfo as PropertyInfo).GetValue(self, null);
                else if (mInfo is FieldInfo)
                    mValue = (mInfo as FieldInfo).GetValue(self);
                else if (mInfo is MethodInfo)
                    mValue = mInfo;
                else if (mInfo is Type)
                    mValue = mInfo;
                else
                    throw new Exception("not a valid member");
                return new Instance(mValue, null);
            }
            else if (name.GetType().IsAssignableFrom(typeof(int)))
            {
                return null;///TODO
            }
            else
                throw new Exception("not a valid member");
        }

        public void SetMember(object name, Instance inst)
        {
            Instance foo;
            if (members != null && members.TryGetValue(name, out foo))
            {
                members[name] = inst;
                return;
            }
            var oInst = GetValue();
            if (oInst != null)
            {
                var oType = (oInst is Type) ? (Type)oInst : oInst.GetType();
                var self = (oInst is Type) ? null : oInst;
                if (name is string)
                {
                    var mInfo = oType.GetMember((string)name).FirstOrDefault();
                    if (mInfo != null)
                    {
                        if (mInfo is PropertyInfo)
                        {
                            (mInfo as PropertyInfo).SetValue(self, inst.Scalar, null);
                            return;
                        }
                        else if (mInfo is FieldInfo)
                        {
                            (mInfo as FieldInfo).SetValue(self, inst.Scalar);
                            return;
                        }
                        else if (mInfo is MethodInfo)
                            throw new Exception("readonly");
                        else if (mInfo is Type)
                            throw new Exception("readonly");
                    }
                }
                else if (name.GetType().IsAssignableFrom(typeof(int)))
                {
                    var mInfo = oType.GetDefaultMembers().FirstOrDefault();
                }
            }
            (members ?? (members = new Dictionary<object, Instance>()))[name] = inst;
        }

        public bool DeleteMember(object name)
        {
            return members != null ? members.Remove(name) : false;
        }

        public Instance[] GetMembers()
        {
            if (members == null)
                return null;
            var memberArray = members.ToArray();
            var instArray = new Instance[memberArray.Length];
            for (var i = 0; i < memberArray.Length; i++)
                instArray[i] = memberArray[i].Value;
            return instArray;
        }

        public void CopyMembers(Instance from)
        {
            if (from != null && from.members != null && from.members.Count > 0)
            {
                if (this.members == null)
                    this.members = new Dictionary<object, Instance>();
                foreach (var m in from.members)
                    this.members.Add(m.Key, m.Value);
                ///TODO os valores copiados, se sofrerem alteracao, refletem no prototype de volta?
            }
        }
    }

    public class Instance : DynamicRecord
    {
        private dynamic scalar;
        private NameContext parentContext;

        public dynamic Scalar { get { return this.scalar; } }
        public NameContext ParentContext { get { return this.parentContext; } }

        protected override object GetValue() { return this.scalar; }

        public Instance() { }
        public Instance(dynamic scalar, NameContext parentContext)
        {
            this.scalar = scalar;
            this.parentContext = parentContext;
        }

        public Instance CreateNew(NameContext context, Instance[] args)
        {
            if (this.scalar is Type)
            {
                var scalarType = this.scalar as Type;
                var scalarArgs = ToScalars(args);
                var obj = Activator.CreateInstance(scalarType, scalarArgs, null);
                return new Instance(obj, null);
            }
            else if (this.scalar is FunctionCode)
            {
                var instance = new Instance(null, context);
                var prototype = this.GetMember("prototype");
                if (prototype != null)
                    instance.CopyMembers(prototype);
                Execute(this.parentContext, instance, args);
                return instance;
            }
            else
                throw new Exception("not a class to create");
        }

        public ExitResult Execute(NameContext context, Instance owner, Instance[] args)
        {
            if (this.scalar is MethodInfo)
            {
                var proc = this.scalar as MethodInfo;
                var scalarArgs = ToScalars(args);
                var instance = owner != null ? (object)owner.Scalar : null;
                var ret = proc.Invoke(instance, scalarArgs);
                return new ExitResult { ExitMode = ExitMode.Normal, ExitValue = new Instance(ret, null) };
            }
            else if (this.scalar is FunctionCode)
            {
                var proc = this.scalar as FunctionCode;
                // nesse callContext serao guardados os parametros e o simbolo this=owner
                var callContext = this.ParentContext.NewChild();
                if (proc.Params != null)
                    for (var i = 0; i < proc.Params.Length; i++)
                        callContext.SetMember(proc.Params[i], args != null && i < args.Length ? args[i] : null);
                if (owner != null)
                    callContext.SetMember("this", owner);
                return proc.Code.Run(callContext);
            }
            else if (this.scalar is Type)
            {
                if (args != null && args.Length > 0)
                {
                    var proc = (this.scalar as Type).GetMethod("Unboxing");
                    if (proc == null)
                        throw new Exception("no castfrom method");
                    var scalarArgs = ToScalars(args);
                    var ret = proc.Invoke(null, scalarArgs);
                    return new ExitResult { ExitMode = ExitMode.Normal, ExitValue = new Instance(ret, null) };
                }
                else
                {
                    var inst = CreateNew(context, args);
                    return new ExitResult { ExitMode = ExitMode.Normal, ExitValue = inst };
                }
            }
            else
                throw new Exception("not a method to call");
        }

        public override string ToString()
        {
            return Scalar != null ? Scalar.ToString() : "null";
        }

        private object[] ToScalars(Instance[] args)
        {
            if (args == null)
                return null;
            var scalarArgs = new object[args.Length];
            for (var i = 0; i < args.Length; i++)
                scalarArgs[i] = args[i].Scalar;
            return scalarArgs;
        }

    }

    public interface ICallable { }

    public class NameContext : DynamicRecord
    {
        private NameContext parentContext;
        public NameContext ParentContext { get { return this.parentContext; } }

        public NameContext NewChild()
        {
            return new NameContext { parentContext = this };
        }

        protected List<Instance> nsList;

        public void UseNS(object ns)
        {
            (nsList ?? (nsList = new List<Instance>())).Add(new Instance(ns, null));
        }

        public override Instance GetMember(object name)
        {
            var member = base.GetMember(name);
            return member ?? ScanNSList(name);
        }

        private Instance ScanNSList(object name)
        {
            if (name is string && nsList != null)
                foreach (var ns in nsList)
                {
                    var inst = ns.GetMember(name);
                    if (inst != null)
                        return inst;
                }
            return null;
        }
    }
}
