using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OopDb
{
    public class Validator
    {
        public static void ValidateObj(object input)
        {
            var attrs = Attribute.GetCustomAttributes(input.GetType()).ToList().OfType<IFilterAttr>().ToList();

            foreach (var item in attrs)
            {
                item.FilterOrValidate(input);
            }

            var props = input.GetType().GetProperties();

            foreach (var prop in props)
            {
                var propAttrs = prop.GetCustomAttributes(true).OfType<IFilterAttr>().ToList();

                var value = prop.GetValue(input);

                foreach (var item in propAttrs)
                {
                    value = item.FilterOrValidate(value);
                }
            }
        }
    }

    public class DbFramework
    {
        public static string DefaultDateTimeFormat(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public Type Type { get; set; }

        public ITableScript TableScriptRules { get; set; }
        public IFieldScript FieldScriptRules { get; set; }
        public IConstraintScript ConstraintScriptRules { get; set; }
        public IForeignScript ForeignScriptRules { get; set; }

        public DbFramework(Type type)
        {
            Type = type;
        }

        public void SetRules(ITableScript tableRules = null, IFieldScript fieldRules = null, IConstraintScript constraintRules = null, IForeignScript foreignRules = null)
        {
            TableScriptRules = tableRules ?? new TableScriptDefaultRules();
            FieldScriptRules = fieldRules ?? new FieldScriptDefaultRules();
            ConstraintScriptRules = constraintRules ?? new ConstraintScriptDefaultRules();
            ForeignScriptRules = foreignRules ?? new ForeignScriptDefaultRules();
        }

        public string GetScript(bool withDependences = false)
        {
            var script = "";            

            var props = Type.GetProperties();

            TableScriptRules.SetType(Type);
            var table = TableScriptRules.GetTableName();

            var fields = new List<string>();
            var constraints = new List<string>();
            var foreignKeys = new List<string>();

            foreach (var prop in props)
            {
                FieldScriptRules.SetProperty(prop);
                ConstraintScriptRules.SetProperty(prop);
                ConstraintScriptRules.SetType(Type);
                ForeignScriptRules.SetProperty(prop);
                ForeignScriptRules.SetType(Type);

                fields.Add(FieldScriptRules.GetField());

                foreach (var item in prop.GetCustomAttributes().OfType<IConfigConstraintAttr>())
                {
                    ConstraintScriptRules.SetConstraintType(item);
                    constraints.Add(ConstraintScriptRules.GetConstraint());
                }

                foreach (var item in prop.GetCustomAttributes().OfType<IForeignKeyAttr>())
                {
                    foreignKeys.Add(ForeignScriptRules.GetForeignConstraint());
                    if (withDependences)
                    {
                        var tempScript = ForeignScriptRules.GetDependencesScript(this) + Environment.NewLine + Environment.NewLine;
                        if (!script.Contains(tempScript))
                        {
                            script += tempScript;
                        }
                    }

                }
            }

            var fieldScript = "";
            var constraintScript = "";
            var foreignKeyScript = "";

            fields.ForEach(f => fieldScript += "    " + f + "," + Environment.NewLine);
            constraints.ForEach(f => constraintScript += "    " + f + "," + Environment.NewLine);
            foreignKeys.ForEach(f => foreignKeyScript += "    " + f + "," + Environment.NewLine);

            script += string.Format(@"CREATE TABLE {1} {0}({0}{2}{0}{3}{0}{4}{0}", Environment.NewLine, table, fieldScript, constraintScript, foreignKeyScript).TrimEnd().TrimEnd(',') + Environment.NewLine + ")" + Environment.NewLine;

            script += TableScriptRules.GetTableDescription();

            return script;
        }
        public void GenerateTables(IDbConnection dbConnection, params Type[] classes)
        {
            dbConnection.ConnectDataBase();
            dbConnection.ForNonQuery(GetScript(true)).Inject();
            dbConnection.DisconnectDataBase();
        }

        public static string DescriptionOrDefault(Type type)
        {
            var attr = Attribute.GetCustomAttributes(type).ToList().OfType<IDescriptionAttr>().FirstOrDefault();
            if (attr != null)
            {
                var script = @"EXEC sys.sp_updateextendedproperty 
                                @name=N'MS_Description', @value=N'{0}' ,
	                            @level0type=N'SCHEMA',@level0name=N'dbo', 
	                            @level1type=N'TABLE'
	                           ,@level1name=N'{1}'";
                return string.Format(script, attr.GetDescription(), AliasOrDefault(type));
            }
            else
                return "";
        }
        public static string AliasOrDefault(Type type)
        {
            var attr = Attribute.GetCustomAttributes(type).ToList().OfType<IAliasAttr>().FirstOrDefault();
            if (attr != null)
                return attr.GetAlias();
            else
                return type.Name;
        }

        public static string AliasOrDefaultField(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttributes(true).OfType<IAliasAttr>().FirstOrDefault();
            if (attr != null)
            {
                return attr.GetAlias();
            }

            var foreignAttr = prop.GetCustomAttributes(true).OfType<IForeignKeyAttr>().FirstOrDefault();
            if (foreignAttr != null)
            {
                return foreignAttr.GetLocalFieldName();
            }

            return prop.Name;
        }

        public static string TypeOrDefault(PropertyInfo propInfo)
        {
            var dataType = propInfo.GetCustomAttributes(true).OfType<IDataTypeAttr>().FirstOrDefault();
            if (dataType != null)
                return dataType.GetType();

            var dataTypeSettings = propInfo.GetCustomAttributes(true).OfType<IDataTypeSettingsAttr>().FirstOrDefault();


            if (propInfo.PropertyType == typeof(string))
            {
                var maxLength = "255";

                if (dataTypeSettings != null && typeof(IMaxLengthAttr).IsAssignableFrom(dataTypeSettings.GetType()))
                {
                    maxLength = ((IMaxLengthAttr)dataTypeSettings).GetLength().ToString();
                }
                return string.Format("varchar({0})", maxLength);
            }
            else if (propInfo.PropertyType == typeof(char))
            {
                return "char(1)";
            }
            else if (propInfo.PropertyType == typeof(Guid))
            {
                return "uniqueidentifier";
            }
            else if (propInfo.PropertyType == typeof(int))
            {
                return "int";
            }
            else if (propInfo.PropertyType == typeof(double) || propInfo.PropertyType == typeof(float) || propInfo.PropertyType == typeof(decimal))
            {

                if (dataTypeSettings != null && typeof(IPrecisionAttr).IsAssignableFrom(dataTypeSettings.GetType()))
                {
                    var precision = ((IPrecisionAttr)dataTypeSettings);
                    return string.Format("numeric({0}, {1})", precision.GetScale().ToString(), precision.GetPrecision().ToString());
                }

                return "float";
            }
            else if (propInfo.PropertyType == typeof(DateTime))
            {
                return "datetime";
            }
            else if (propInfo.PropertyType == typeof(bool))
            {
                return "bit";
            }
            else
            {
                var foreignAttr = propInfo.GetCustomAttributes(true).OfType<IForeignKeyAttr>().FirstOrDefault();
                if (foreignAttr == null) return "varchar(50)";
                var foreignType = propInfo.PropertyType;
                var referenceType = foreignType.GetProperty(foreignAttr.GetReferenceFieldName());
                return TypeOrDefault(referenceType);
            }
        }

        public static bool IsRequiredOrDefault(PropertyInfo propInfo)
        {
            return propInfo.GetCustomAttributes(true).OfType<IRequiredAttr>().FirstOrDefault() != null;
        }

        public static string HasDefaultOrDefaultRule(PropertyInfo propInfo)
        {
            var value = propInfo.GetCustomAttributes(true).OfType<IDefaultAttr>().FirstOrDefault();
            if (value != null)
                return value.GetDefault();

            return "";
        }

        public static string ForeignKeyLocalFieldOrDefaultRule(PropertyInfo propInfo)
        {
            return propInfo.GetCustomAttributes().OfType<IForeignKeyAttr>().FirstOrDefault().GetLocalFieldName();
        }

        public static string ForeignKeyReferenceFieldOrDefaultRule(PropertyInfo propInfo)
        {
            var foreignSettings = propInfo.GetCustomAttributes().OfType<IForeignKeyAttr>().FirstOrDefault();
            var type = propInfo.PropertyType;
            var prop = type.GetProperty(foreignSettings.GetReferenceFieldName());
            return AliasOrDefaultField(prop);
        }

    }

    public class Db
    {
        public IDbConnection DbConnection { get; set; }
        public Db(IDbConnection connection)
        {
            DbConnection = connection;
        }
    }

    #region DataBase Interfaces

    public interface IDbConnection : IDisposable
    {
        void ConnectDataBase();
        IQuery ForQuery(string sql);
        INonQuery ForNonQuery(string sql);
        void DisconnectDataBase();
    }

    public interface IQuery
    {
        string GetSql();
        void SetSql();
        T First<T>(params object[] args);
        List<T> All<T>(params object[] args);
    }

    public interface INonQuery
    {
        string GetSql();
        void SetSql();
        void Inject(params object[] args);
    }

    #endregion

    #region Interfaces Rules

    public interface ITableScript
    {
        void SetType(Type type);

        string GetTableName();
        string GetTableDescription();
    }

    public interface IFieldScript
    {
        string GetField();
        void SetProperty(PropertyInfo property);
    }

    public interface IConstraintScript
    {
        string GetConstraint();
        void SetType(Type type);
        void SetProperty(PropertyInfo property);
        void SetConstraintType(IConfigConstraintAttr property);
    }

    public interface IForeignScript
    {
        string GetDependencesScript(DbFramework dbFramework);
        string GetForeignConstraint();
        void SetProperty(PropertyInfo property);
        void SetType(Type type);
    }

    #endregion

    #region Default Rules

    public class TableScriptDefaultRules : ITableScript
    {
        public Type Type { get; set; }

        public void SetType(Type type)
        {
            Type = type;
        }

        public string GetTableName()
        {
            return DbFramework.AliasOrDefault(Type);
        }

        public string GetTableDescription()
        {
            return DbFramework.DescriptionOrDefault(Type);
        }
    }
    public class FieldScriptDefaultRules : IFieldScript
    {
        public PropertyInfo Property { get; set; }

        public string GetField()
        {
            return string.Format("{0} {1} {2} {3}", GetFieldName(), GetFieldType(), GetRequired(), GetDefault()).Trim();
        }

        public string GetFieldName()
        {
            return DbFramework.AliasOrDefaultField(Property);
        }

        public string GetFieldType()
        {
            return DbFramework.TypeOrDefault(Property);
        }

        public string GetRequired()
        {
            return DbFramework.IsRequiredOrDefault(Property) ? "NOT NULL" : "";
        }

        public string GetDefault()
        {
            return DbFramework.HasDefaultOrDefaultRule(Property);
        }

        public void SetProperty(PropertyInfo property)
        {
            Property = property;
        }
    }
    public class ConstraintScriptDefaultRules : IConstraintScript
    {
        public Type Type { get; set; }
        public PropertyInfo Property { get; set; }
        public IConfigConstraintAttr ConstraintType { get; set; }

        public string GetConstraint()
        {
            return string.Format("CONSTRAINT {0} {1} ({2})", GetConstraintName(), GetConstraintType(), GetConstraintField());
        }

        public string GetConstraintField()
        {
            return DbFramework.AliasOrDefaultField(Property);
        }

        public string GetConstraintName()
        {
            if (typeof(IUniqueAttr).IsAssignableFrom(ConstraintType.GetType()))
            {
                return string.Format("UN_{0}_{1}", DbFramework.AliasOrDefault(Type), DbFramework.AliasOrDefaultField(Property));
            }
            if (typeof(IKeyAttr).IsAssignableFrom(ConstraintType.GetType()))
            {
                return string.Format("PK_{0}_{1}", DbFramework.AliasOrDefault(Type), DbFramework.AliasOrDefaultField(Property));
            }
            if (typeof(IClusterAttr).IsAssignableFrom(ConstraintType.GetType()))
            {
                return string.Format("IX_{0}_{1}", DbFramework.AliasOrDefault(Type), DbFramework.AliasOrDefaultField(Property));
            }
            else
            {
                return "";
            }
        }

        public string GetConstraintType()
        {
            if (typeof(IUniqueAttr).IsAssignableFrom(ConstraintType.GetType()))
            {
                return "UNIQUE";
            }
            if (typeof(IKeyAttr).IsAssignableFrom(ConstraintType.GetType()))
            {
                return "PRIMARY KEY";
            }
            if (typeof(IClusterAttr).IsAssignableFrom(ConstraintType.GetType()))
            {
                return "CLUSTERED";
            }
            else
            {
                return "";
            }
        }

        public void SetType(Type type)
        {
            Type = type;
        }
        public void SetProperty(PropertyInfo property)
        {
            Property = property;
        }
        public void SetConstraintType(IConfigConstraintAttr constraintType)
        {
            ConstraintType = constraintType;
        }
    }
    public class ForeignScriptDefaultRules : IForeignScript
    {
        public PropertyInfo Property { get; set; }
        public Type Type { get; set; }

        public string GetForeignConstraint()
        {
            return string.Format("CONSTRAINT FK_{0}_{2} FOREIGN KEY ({1}) REFERENCES {2}({3})", GetLocalTableName(), GetForeignLocalFieldName(), GetReferenceTableName(), GetReferenceFieldName());
        }
        public string GetForeignLocalFieldName()
        {
            return DbFramework.ForeignKeyLocalFieldOrDefaultRule(Property);
        }
        public string GetReferenceFieldName()
        {
            return DbFramework.ForeignKeyReferenceFieldOrDefaultRule(Property);
        }
        public string GetReferenceTableName()
        {
            return DbFramework.AliasOrDefault(Property.PropertyType);
        }
        public string GetLocalTableName()
        {
            return DbFramework.AliasOrDefault(Type);
        }

        public void SetProperty(PropertyInfo property)
        {
            Property = property;
        }
        public void SetType(Type type)
        {
            Type = type;
        }

        public string GetDependencesScript(DbFramework dbFramework)
        {
            var db = new DbFramework(Property.PropertyType);

            var tableRules = (ITableScript)Activator.CreateInstance(dbFramework.TableScriptRules.GetType());
            var fieldRules = (IFieldScript)Activator.CreateInstance(dbFramework.FieldScriptRules.GetType());
            var constraintRules = (IConstraintScript)Activator.CreateInstance(dbFramework.ConstraintScriptRules.GetType());
            var foreignkeyRules = (IForeignScript)Activator.CreateInstance(dbFramework.ForeignScriptRules.GetType());

            db.SetRules(tableRules, fieldRules, constraintRules, foreignkeyRules);
            return db.GetScript(true);
        }
    }

    #endregion

    #region Interface Attributes

    //Most interface is to organization only

    public interface IFilterAttr
    {
        object FilterOrValidate(object input);
    }

    public interface IValidatesAttr : IFilterAttr
    {
        void Validate(object input);
    }

    public interface IConfigAttr
    {
    }

    public interface IConfigTableAttr : IConfigAttr
    {
    }

    public interface IConfigFieldAttr : IConfigAttr
    {
    }

    public interface IDataTypeAttr : IConfigFieldAttr
    {
        string GetType();
    }

    public interface IDefaultAttr : IConfigFieldAttr
    {
        string GetDefault();
    }


    public interface IDataTypeSettingsAttr : IConfigFieldAttr
    {
    }

    public interface IPrecisionAttr : IDataTypeSettingsAttr
    {
        int GetScale();
        int GetPrecision();
    }

    public interface IMaxLengthAttr : IDataTypeSettingsAttr, IValidatesAttr
    {
        int GetLength();
    }

    public interface IAliasAttr : IConfigFieldAttr, IConfigTableAttr
    {
        string GetAlias();
    }

    public interface IDescriptionAttr : IConfigTableAttr
    {
        string GetDescription();
    }

    public interface IRequiredAttr : IConfigFieldAttr
    {
    }

    public interface IKeyAttr : IConfigConstraintAttr
    {
    }

    public interface IUniqueAttr : IConfigConstraintAttr
    {
    }

    public interface IClusterAttr : IConfigConstraintAttr
    {
    }

    public interface IForeignKeyAttr : IConfigForeignAttr, IOutputAttr, IInputAttr
    {
        string GetLocalFieldName();
        string GetReferenceFieldName();
    }

    public interface IConfigForeignAttr : IConfigAttr
    {
    }

    public interface IConfigConstraintAttr : IConfigAttr
    {
    }

    public interface IOutputAttr : IFilterAttr
    {
        object GetOutput(object input);
    }

    public interface IInputAttr
    {
        void SetInput(PropertyInfo property, object instace, object input);
    }

    public interface IFormatAttr : IOutputAttr {
        string Format(object input);
    }
    public interface IDateTimeFormat : IFormatAttr
    {
        new string Format(object input);
    }

    public interface ISubstitute : IFormatAttr
    {
        new string Format(object input);
    }

    #endregion

    #region Default Attributes

    #region Script Attributes

    public class AliasAttribute : Attribute, IAliasAttr
    {
        public string Alias { get; private set; }
        public AliasAttribute(string alias)
        {
            Alias = alias;
        }

        public string GetAlias()
        {
            return Alias;
        }
    }

    public class DescriptionAttribute : Attribute, IDescriptionAttr
    {
        public string Description { get; private set; }
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string GetDescription()
        {
            return Description;
        }
    }

    public class DataTypeAttribute : Attribute, IDataTypeAttr
    {
        public string Type { get; private set; }
        public DataTypeAttribute(string type)
        {
            Type = type;
        }

        string IDataTypeAttr.GetType()
        {
            return Type;
        }
    }

    public class CreateAsAttribute : Attribute
    {
        public string CreateAs { get; private set; }
        public CreateAsAttribute(string createAs)
        {
            CreateAs = createAs;
        }
    }

    public class IsKeyAttribute : Attribute, IValidatesAttr, IKeyAttr
    {
        public string ConstraintName { get; private set; }
        public IsKeyAttribute(string constraintName)
        {
            ConstraintName = constraintName;
        }

        public IsKeyAttribute()
        {
            ConstraintName = "PK_DEFAULT";
        }

        public void Validate(object input)
        {
            if (input == null || (string.IsNullOrWhiteSpace(input.ToString())))
            {
                throw new Exception("Value can't be empty or null");
            }
        }

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
        }
    }

    public class IsRequiredAttribute : Attribute, IValidatesAttr, IRequiredAttr
    {
        public void Validate(object input)
        {
            if (input == null || (string.IsNullOrWhiteSpace(input.ToString())))
            {
                throw new Exception("Value can't be empty or null");
            }
        }
        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
        }
    }

    public class DefaultAttribute : Attribute, IDefaultAttr
    {
        public object Default { get; set; }
        public DefaultAttribute(object value)
        {
            Default = value;
        }
        public string GetDefault()
        {
            return string.Format("Default({0})", Default);
        }
    }

    public class IsUniqueAttribute : Attribute, IUniqueAttr
    {
        public string ConstraintName { get; private set; }
        public IsUniqueAttribute(string constraintName)
        {
            ConstraintName = constraintName;
        }

        public IsUniqueAttribute()
        {
            ConstraintName = "UN_DEFAULT";
        }
    }

    public class IsClusteredAttribute : Attribute, IClusterAttr
    {
        public string ConstraintName { get; private set; }
        public IsClusteredAttribute(string constraintName)
        {
            ConstraintName = constraintName;
        }
        public IsClusteredAttribute()
        {
            ConstraintName = "IX_DEFAULT";
        }
    }

    public class IsForeignAttribute : Attribute, IValidatesAttr, IForeignKeyAttr
    {
        public string ConstraintName { get; private set; }
        public string Name { get; set; }
        public string ReferenceName { get; set; }
        public object ForeignKeyId { get; set; }
        public IsForeignAttribute(string name, string referenceName, string constraintName)
        {
            Name = name;
            ReferenceName = referenceName;
            ConstraintName = constraintName;
        }
        public IsForeignAttribute(string name, string referenceName)
        {
            Name = name;
            ReferenceName = referenceName;
            ConstraintName = "FK_DEFAULT_" + name + "_" + referenceName;
        }
        public void Validate(object input)
        {
            Validator.ValidateObj(input);
        }

        public string GetLocalFieldName()
        {
            return Name;
        }

        public string GetReferenceFieldName()
        {
            return ReferenceName;
        }

        public void SetForeignKeyId(object id)
        {
            ForeignKeyId = id;
        }

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
        }

        public object GetOutput(object input)
        {
            var type = input.GetType();
            return type.GetProperty(GetReferenceFieldName()).GetValue(input);
        }

        public void SetInput(PropertyInfo property, object instace, object input)
        {
            SetForeignKeyId(input);
        }
    }

    public class PrecisionAttribute : Attribute, IPrecisionAttr
    {
        public int Scale { get; private set; }
        public int Precision { get; private set; }
        public PrecisionAttribute(int scale, int precision)
        {
            Scale = scale;
            Precision = precision;
        }

        public int GetScale()
        {
            return Scale;
        }

        public int GetPrecision()
        {
            return Precision;
        }
    }

    public class MaxLenghtAttribute : Attribute, IMaxLengthAttr
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

        public int GetLength()
        {
            return MaxLength;
        }

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
        }
    }
    #endregion

    #region Business Rules Only


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

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
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
                throw new Exception(string.Format("Value can't be greater or equals than {0}", LessThan));
            }
        }

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
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
                throw new Exception(string.Format("Value can't be less or equals than {0}", GreaterThan));
            }
        }

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
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
            if (!Checks.Select(s => s.ToString()).Contains(input.ToString()))
                throw new Exception("Value is invalid");
        }

        public object FilterOrValidate(object input)
        {
            Validate(input);
            return input;
        }
    }

    public class DefaultDateTimeFormat : Attribute, IDateTimeFormat
    {
        public string Format(object input)
        {
            return DbFramework.DefaultDateTimeFormat((DateTime)input);
        }

        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    public class DateTimeFormat : Attribute, IDateTimeFormat
    {
        public string FormatValue { get; set; }
        public DateTimeFormat(string format)
        {
            FormatValue = format;
        }
        public string Format(object input)
        {
            return ((DateTime)input).ToString(FormatValue);
        }
        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    public class NumberFormat : Attribute, IFormatAttr
    {
        public string FormatValue { get; set; }
        public NumberFormat(string format)
        {
            FormatValue = format;
        }
        public string Format(object input)
        {
            return Convert.ToDouble(input).ToString(FormatValue);
        }
        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    public class ToUpper : Attribute, IFormatAttr
    {
        public string Format(object input)
        {
            return input.ToString().ToUpper();
        }
        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    public class ToLower : Attribute, IFormatAttr
    {
        public string Format(object input)
        {
            return input.ToString().ToLower();
        }
        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    public class Substring : Attribute, IFormatAttr
    {
        public int Start { get; set; }
        public int End { get; set; }
        public Substring(int start)
        {
            Start = start;
        }
        public Substring(int start, int end)
        {
            Start = start;
            End = end;
        }
        public string Format(object input)
        {
            if(End <= 0)
                return input.ToString().Substring(Start);
            else
                return input.ToString().Substring(Start, End);
        }
        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    public class Replace : Attribute, IFormatAttr
    {
        public string Old { get; set; }
        public string New { get; set; }
        public Replace(string oldValue, string newValue = "")
        {
            New = newValue;
            Old = oldValue;
        }
        public string Format(object input)
        {
            return input.ToString().Replace(Old, New);
        }
        public object GetOutput(object input)
        {
            return Format(input);
        }
        public object FilterOrValidate(object input)
        {
            return GetOutput(input);
        }
    }

    #endregion

    #endregion



}