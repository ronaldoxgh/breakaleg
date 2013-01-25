namespace Breakaleg.Core.Dynamic
{
    public interface ICallable { }

    public class NameContext : DynamicRecord
    {
        private NameContext _parentContext;
        public NameContext ParentContext { get { return this._parentContext; } }

        public NameContext NewChild()
        {
            return new NameContext
            {
                _parentContext = this
            };
        }

        public Instance GetFieldUpwards(object name)
        {
            var temp = this;
            while (temp != null)
            {
                var field = temp.GetField(name);
                if (field != null)
                    return field;
                temp = temp.ParentContext;
            }
            return null;
        }

        public void SetFieldUpwards(object name, Instance inst)
        {
            // o ultimo contexto recebe todas as variaveis sem dono (equiv.: dhtml.window)
            NameContext defaultContext = null;
            var temp = this;
            while (temp != null)
            {
                if (temp.GetField(name) != null)
                {
                    temp.SetField(name, inst);
                    return;
                }
                defaultContext = temp;
                temp = temp.ParentContext;
            }
            if (defaultContext != null)
                defaultContext.SetField(name, inst);
        }
    }
}
