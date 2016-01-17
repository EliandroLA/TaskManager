using System;
using System.Linq;

namespace OopDb
{
    public class Validator
    {
        public static void ValidateObj(object input)
        {
            var attrs = Attribute.GetCustomAttributes(input.GetType()).ToList().OfType<IValidatesAttr>().ToList();

            foreach (var item in attrs)
            {
                item.Validate(input);
            }

            var props = input.GetType().GetProperties();

            foreach (var prop in props)
            {
                var propAttrs = prop.GetCustomAttributes(true).ToList().OfType<IValidatesAttr>().ToList();

                var value = prop.GetValue(input);

                foreach (var item in propAttrs)
                {
                    item.Validate(value);
                }
            }
        }
    }

    public interface IValidatesAttr
    {
        void Validate(object input);
    }

    public interface IConfigAttr
    {

    }

    public class AliasAttribute : Attribute, IConfigAttr
    {
        public string Alias { get; private set; }
        public AliasAttribute(string alias)
        {
            Alias = alias;
        }
    }

    public class DataTypeAttribute : Attribute, IConfigAttr
    {
        public string Type { get; private set; }
        public DataTypeAttribute(string type)
        {
            Type = type;
        }
    }

    public  class IsKeyAttribute : Attribute, IValidatesAttr, IConfigAttr
    {
        public bool IsKey { get; set; }
        public IsKeyAttribute()
        {
            IsKey = true;
        }

        public void Validate(object input)
        {
            if (input == null || (string.IsNullOrWhiteSpace(input.ToString())))
            {
                throw new Exception("Value can't be empty or null");
            }
        }
    }

    public class IsRequiredAttribute : Attribute, IValidatesAttr, IConfigAttr
    {
        public bool IsKey { get; set; }
        public IsRequiredAttribute()
        {
            IsKey = true;
        }

        public void Validate(object input)
        {
            if (input == null || (string.IsNullOrWhiteSpace(input.ToString())))
            {
                throw new Exception("Value can't be empty or null");
            }
        }
    }

    public class MaxLenghtAttribute : Attribute, IValidatesAttr, IConfigAttr
    {
        public int MaxLength { get; private set; }
        public MaxLenghtAttribute(int maxLenght)
        {
            MaxLength = maxLenght;
        }

        public void Validate(object input)
        {
            if (input.ToString().Length > MaxLength)
            {
                throw new Exception(string.Format("Length can't be greater than {0}", MaxLength));
            }
        }
    }

    public class MinLenghtAttribute : Attribute, IValidatesAttr
    {
        public int MinLength { get; private set; }
        public MinLenghtAttribute(int minLength)
        {
            MinLength = minLength;
        }

        public void Validate(object input)
        {
            if (input.ToString().Length < MinLength)
            {
                throw new Exception(string.Format("Length can't be less than {0}", MinLength));
            }
        }
    }

    public class LessThanAttribute : Attribute, IValidatesAttr
    {
        public double LessThan { get; private set; }
        public LessThanAttribute(double lessThan)
        {
            LessThan = lessThan;
        }

        public void Validate(object input)
        {
            if (Convert.ToDouble(input) >= LessThan)
            {
                throw new Exception(string.Format("Value can't be greater than {0}", LessThan));
            }
        }
    }

    public class GreaterThanAttribute : Attribute, IValidatesAttr
    {
        public double GreaterThan { get; private set; }
        public GreaterThanAttribute(double greaterThan)
        {
            GreaterThan = greaterThan;
        }

        public void Validate(object input)
        {
            if (Convert.ToDouble(input) <= GreaterThan)
            {
                throw new Exception(string.Format("Value can't be less than {0}", GreaterThan));
            }
        }
    }

    public class CheckAttribute : Attribute, IValidatesAttr
    {
        public object[] Checks { get; private set; }
        public CheckAttribute(params object[] checks)
        {
            Checks = checks;
        }

        public void Validate(object input)
        {
            if (!Checks.Contains(input))
                throw new Exception("Value is invalid");
        }
    }

}