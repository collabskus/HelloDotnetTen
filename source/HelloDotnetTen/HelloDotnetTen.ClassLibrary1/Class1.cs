namespace HelloDotnetTen.ClassLibrary1
{
    public class Class1
    {
        private readonly string injectedProperty1;
        public Class1(string injectedProperty1)
        {
            this.injectedProperty1 = injectedProperty1 ?? throw new ArgumentNullException(nameof(injectedProperty1));
        }
        public int GetLengthOfInjectedProperty1()
        {
            return injectedProperty1.Length;
        }
    }
}
