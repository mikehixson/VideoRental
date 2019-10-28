using System;
using System.Collections.Generic;
using System.Text;

namespace VideoRental.Core
{
    public class Video
    {
        public string Id { get; }
        public string Title { get; }
        public string Format { get; }
        public bool IsNew { get; }
        public Preorder Preorder { get; }

        public Video(string id, string title, string format, bool isNew, Preorder preorder = null)
        {
            Id = id;
            Title = title;
            Format = format;
            IsNew = isNew;
            Preorder = preorder;
        }
    }
}
