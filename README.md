﻿# OSU-NetcloudMusic-player
>osu和网易云音乐的播放器，适合使用网易云音乐和osu的用户


**对于网易云音乐它会读取数据从本地缓存，所以对于没有被在本地缓存的歌单，你可能看不见数据**

**对于osu!,他会读取收藏夹和歌曲列表，并在首次更新的时候花费大约3min**

你可以像使用工具一样使用它：
+ 1，导出歌单或者收藏列表
+ 2，批量移除网易云音乐歌单中的可替代的，且有版权的，非无损音乐
+ 3，全局搜索
+ ~~4，歌词批量导出（这个隐藏了）~~

---
*导出的歌单在运行目录下*

*批量移除歌曲时请先更新缓存（先在云音乐中同步本地歌曲，然后再去点你的歌单），移除的歌曲在back目录中备份，移除完成后再次在云音乐中同步本地歌曲即可`*

---

也可以像一般播放器那样使用,它支持了音频播放器的常见功能，以及
+ 音乐笔记
+ 网易云音乐歌单历史
+ 歌曲以文件流的方式加载播放（这个在flac文件中支持不完善）


其他：
1，清除播放列表使用delete和ctrl+delete
2, 滚动左侧歌单列表可以在左侧空白试试，右键返回顶部

3，歌词数据解析是另一个人写的，找不到人了。


