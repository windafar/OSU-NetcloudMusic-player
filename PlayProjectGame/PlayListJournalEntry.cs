using PlayProjectGame.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PlayProjectGame
{
    public delegate void ReplayListChange(PlayListJournalEntry state);
    [Serializable()]
    public class PlayListJournalEntry : CustomContentState
    {
        public PlayListData PLD
        {
            get;
        }
        public object curSelected{ get; }

        public override string JournalEntryName
        {
            get
            {
                return PLD.PlatListName;
            }
        }

        public ReplayListChange replayListChange;
        public override void Replay(NavigationService navigationService, NavigationMode mode)
        {
            this.replayListChange(this);
        }

        public PlayListJournalEntry(PlayListData PLD,ReplayListChange replayListChange, object curSelected = null)
        {
            this.PLD = PLD;
            this.replayListChange = replayListChange;
            this.curSelected = curSelected;
        }
    }
}
