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
                return string.Equals(x.SongInfo.SongPath, y.SongInfo.SongPath);
            }

            public int GetHashCode(SongInfoExpend obj)
            {
                return obj.ToString().GetHashCode();
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
        public static IEqualityComparer<SongInfoExpend> SongPathEqualityComparer { get; } = new _SongPathEqualityComparer();
        public static IComparer<SongInfoExpend> FileCreateTimeEqualityComparer { get; } = new _FileCreateTimeEqualityComparer();
    }
}
