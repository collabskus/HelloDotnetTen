namespace HelloDotnetTen.ClassLibrary1
{
    public class Class2
    {
        private readonly string myInjectedProperty1;
        public Class2(ClassLibrary1Settings settings)
        {
            this.myInjectedProperty1 = settings.InjectedProperty1 ?? throw new ArgumentNullException(nameof(settings.InjectedProperty1));
        }
        public int GetLengthOfInjectedProperty1()
        {
            return myInjectedProperty1.Length;
        }
    }
}
