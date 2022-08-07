namespace PhotoArchive.Models
{
    public class Format
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
        public string To_String
        {
            get
            {
                return ToString();
            }
        }
    }
}
