namespace HelloDotnetTen.ClassLibrary1
{
    public class Class1
    {
        private readonly string injectedProperty1;
        public Class1(ClassLibrary1Settings settings)
        {
            this.injectedProperty1 = settings.InjectedProperty1 ?? throw new ArgumentNullException(nameof(settings.InjectedProperty1));
        }
        public int GetLengthOfInjectedProperty1()
        {
            return injectedProperty1.Length;
        }
    }
}
