using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using PlayProjectGame.Data;

namespace PlayProjectGame.Note
{
    [Serializable]
    public class NotesViewData 
    {
        [NonSerialized]
        const string dir = "musicnote\\";
        public NotesViewData(SongInfo sinfo)
        {
            NotesViewData notesViewData;
            filePath= TestNotePath(sinfo);
            if (filePath != null)
            {
                notesViewData = (NotesViewData)Helper.OtherHelper.DeserializeObject(File.ReadAllBytes(filePath));
                this.Notes = notesViewData.Notes;
            }
            else {
                //notesViewData = new NotesViewData(sinfo);
                this.Notes = new LinkedList<NoteContentViewData>();
            }
        }

        public NotesViewData() { }

        private string filePath;
        
        public LinkedList<NoteContentViewData> Notes { get; set; }
        public void Save(SongInfo sinfo)
        {
            var node = Notes.First;
            //while ((node = node.Next) != null)
            //    if (string.IsNullOrWhiteSpace(node.Value.Content))
            //        node.List.Remove(node);
            var by = Helper.OtherHelper.SerializeObject(this);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (filePath == null || !File.Exists(filePath))
               filePath= dir + Helper.OtherHelper.ReplaceValidFileName(sinfo.SongAlbum + sinfo.SongName);
            File.WriteAllBytes(filePath, by);
            //DataGeter.WriteSerializer(this, typeof(NotesViewData), filePath);

        }
        static string TestNotePath(SongInfo sinfo)
        {
           var path= dir + Helper.OtherHelper.ReplaceValidFileName(sinfo.SongAlbum + sinfo.SongName);
            return File.Exists(path)? path:null
                ;
        }
    }
    [Serializable]
   public class NoteContentViewData : INotifyPropertyChanged
    {
        string noteDate;
        long startTime;
        string content;

        public string NoteDate
        {
            get => noteDate; set
            {
                noteDate = value;
                OnPropertyChanged("NoteDate");
            }
        }
        public long StartTime
        {
            get => startTime; set
            {
                startTime = value;
                OnPropertyChanged("StartTime");
            }
        }
        public string Content
        {
            get => content; set
            {
                content = value;
                OnPropertyChanged("Content");

            }
        }

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
            
        //}

        public NoteContentViewData() { }

        public NoteContentViewData(TimeSpan StartTime,string content) {
            this.content = Content;
            this.noteDate = DateTime.Now.ToLongDateString();
            this.startTime = StartTime.Ticks;
        }
    }
}
