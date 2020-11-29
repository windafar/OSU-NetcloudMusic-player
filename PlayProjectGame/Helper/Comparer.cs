using PlayProjectGame.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayProjectGame.Helper
{
    class Comparer
    {
        private sealed class _SongPathEqualityComparer : IEqualityComparer<SongInfoExpend>
        {
            public bool Equals(SongInfoExpend x, SongInfoExpend y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return
                    x.SongInfo.SongId == y.SongInfo.SongId;
            }

            public int GetHashCode(SongInfoExpend obj)
            {
                return obj.SongInfo.SongId.ToString().GetHashCode();
            }
        }
        private sealed class _FileCreateTimeEqualityComparer : IComparer<SongInfoExpend>
        {
            public int Compare(SongInfoExpend x, SongInfoExpend y)
            {
                if (x.FileTime > y.FileTime)
                    return -1;
                if (x.FileTime == y.FileTime)
                    return 0;
                else return 1;
            }
        }
        private sealed class _PlayListIdComparer : IEqualityComparer<PlayListData>
        {
            public bool Equals(PlayListData x, PlayListData y)
            {
                return x.PlayListId == y.PlayListId;
            }

            public int GetHashCode(PlayListData obj)
            {
                return obj.PlayListId.GetHashCode();
            }
        }



        public static IEqualityComparer<SongInfoExpend> SongPathEqualityComparer { get; } = new _SongPathEqualityComparer();
        public static IComparer<SongInfoExpend> FileCreateTimeEqualityComparer { get; } = new _FileCreateTimeEqualityComparer();
        public static IEqualityComparer<PlayListData> PlayListIdComparer { get; } = new _PlayListIdComparer();
        public static IEqualityComparer<UserData> UserIdComparer { get; } = new _UserIdComparer();
        private class _UserIdComparer : IEqualityComparer<UserData>
        {
            public bool Equals(UserData x, UserData y)
            {
                return x.Uid == y.Uid;
            }

            public int GetHashCode(UserData obj)
            {
                return obj.Uid.GetHashCode();
            }
        }
    }
}
