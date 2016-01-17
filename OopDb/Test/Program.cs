using OopDb;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new User();
            a.Value = 9;
            a.Name = "123456";
            a.Status = 'R';

            Validator.ValidateObj(a);

            System.Console.ReadKey();

        }


    }

    [Alias("Teste")]
    class User
    {
        [GreaterThan(0), LessThan(10), DataType("float")]
        public int Value { get; set; }
        [MinLenght(6), MaxLenght(10)]
        public string Name { get; set; }
        [Check('R','S','W')]
        public char Status { get; set; }
    }

    
}
