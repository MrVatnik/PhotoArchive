namespace PhotoArchive.Models
{
    public class Camera
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int FormatId { get; set; }
        public Format? Format { get; set; }

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
