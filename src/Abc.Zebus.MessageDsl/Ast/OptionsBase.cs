using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Abc.Zebus.MessageDsl.Ast;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class OptionsBase
{
    public OptionDescriptor? GetOptionDescriptor(string? optionName)
    {
        if (string.IsNullOrEmpty(optionName))
            return null;

        var property = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .FirstOrDefault(prop => prop.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase));

        if (property == null)
            return null;

        return new OptionDescriptor(this, property);
    }

    public class OptionDescriptor
    {
        private readonly OptionsBase _options;
        private readonly PropertyInfo _property;

        public string Name => _property.Name;

        public bool IsBoolean => _property.PropertyType == typeof(bool);

        internal OptionDescriptor(OptionsBase options, PropertyInfo property)
        {
            _options = options;
            _property = property;
        }

        public bool SetValue(string value)
        {
            try
            {
                var typedValue = Convert.ChangeType(value, _property.PropertyType);
                _property.SetValue(_options, typedValue);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
