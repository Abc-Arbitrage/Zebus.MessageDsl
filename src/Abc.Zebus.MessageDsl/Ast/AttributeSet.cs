using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class AttributeSet : AstNode, IList<AttributeDefinition>
    {
        public IList<AttributeDefinition> Attributes { get; } = new List<AttributeDefinition>();

        public AttributeDefinition GetAttribute(TypeName attributeType)
        {
            attributeType = AttributeDefinition.NormalizeAttributeTypeName(attributeType);
            return Attributes.FirstOrDefault(attr => Equals(attr.TypeName, attributeType));
        }

        public bool HasAttribute(TypeName attributeType) => GetAttribute(attributeType) != null;

        public void AddFlagAttribute(TypeName attributeType)
        {
            if (!HasAttribute(attributeType))
                Attributes.Add(new AttributeDefinition(attributeType));
        }

        public AttributeSet Clone()
        {
            var newSet = new AttributeSet();

            foreach (var attribute in Attributes)
                newSet.Attributes.Add(attribute.Clone());

            return newSet;
        }

        public IEnumerator<AttributeDefinition> GetEnumerator() => Attributes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Attributes).GetEnumerator();
        public void Add(AttributeDefinition item) => Attributes.Add(item);
        public void Clear() => Attributes.Clear();
        public bool Contains(AttributeDefinition item) => Attributes.Contains(item);
        public void CopyTo(AttributeDefinition[] array, int arrayIndex) => Attributes.CopyTo(array, arrayIndex);
        public bool Remove(AttributeDefinition item) => Attributes.Remove(item);
        public int Count => Attributes.Count;
        public bool IsReadOnly => Attributes.IsReadOnly;
        public int IndexOf(AttributeDefinition item) => Attributes.IndexOf(item);
        public void Insert(int index, AttributeDefinition item) => Attributes.Insert(index, item);
        public void RemoveAt(int index) => Attributes.RemoveAt(index);

        public AttributeDefinition this[int index]
        {
            get => Attributes[index];
            set => Attributes[index] = value;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[");

            if (Attributes.Count > 0)
            {
                foreach (var attr in Attributes)
                    sb.Append(attr).Append(", ");
                sb.Length -= 2;
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
