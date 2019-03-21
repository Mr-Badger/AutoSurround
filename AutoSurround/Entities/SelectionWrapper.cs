namespace AutoSurround.Entities
{
    class SelectionWrapper
    {
        public string OpenTag { get; set; }

        public string CloseTag { get; set; }

        public SelectionWrapper(string openTag, string closeTag)
        {
            OpenTag = openTag;
            CloseTag = closeTag;
        }
    }
}
