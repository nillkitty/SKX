using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SKX
{
    public class Bundle
    {
        /// <summary>
        /// The layouts stored within the bundle
        /// </summary>
        public List<Layout> Layouts { get; } = new List<Layout>();

        /// <summary>
        /// The recorded demos within the bundle
        /// </summary>
        public List<Demo> Demos { get; } = new List<Demo>();

        /// <summary>
        /// Loads a room layout from the bundle
        /// </summary>
        /// <param name="story"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public Layout LoadRoom(Story story, int room)
        {
            var y = Layouts.FirstOrDefault(x => x.RoomNumber == room && x.Story == story);
            if (y is null) return null;

            // Clone it so changes don't stick!
            var l = new Layout(y.World, y.Width, y.Height);
            JsonConvert.PopulateObject(y.ToJSON(), l);
            return l;
        }

        /// <summary>
        /// Gets the name of a room given a story and room number.
        /// </summary>
        /// <returns>Returns null if room not found or has no name</returns>
        public string GetRoomName(Story story, int roomNum)
        {
            return Layouts.Where(r => r.RoomNumber == roomNum && r.Story == story)
                          .Select(r => r.Name).FirstOrDefault();
        }

        /// <summary>
        /// Builds a bundle representing all of the room files in the work directory.
        /// </summary>
        public static Bundle Build(bool combine)
        {
            var b = new Bundle();

            if (combine)
            {
                b.Demos.AddRange(Game.Assets.Bundle.Demos);
                b.Layouts.AddRange(Game.Assets.Bundle.Layouts);
            }

            var rooms = GetWorkFiles<Layout>("room", z => Layout.LoadFile(z, null));
            var demos = GetWorkFiles<Demo>("demo");

            foreach (var r in rooms) {
                b.Layouts.RemoveAll(x => x.RoomNumber == r.RoomNumber && x.Story == r.Story);
                b.Layouts.Add(r);
            }
            foreach(var d in demos)
            {
                b.Demos.RemoveAll(x => x.RoomNumber == d.RoomNumber && x.Story == d.Story);
                b.Demos.Add(d);
            }

            return b;
        }

        internal static IEnumerable<T> GetWorkFiles<T>(string prefix) where T : BundleItem, new()
        {
            string regex = $@"^{prefix}_([0-9a-fA-F]+)(c|p|t|x)\.json$";
            var x = Directory.GetFiles(Game.AppDirectory, prefix + "*.json");
            foreach (var f in x)
            {
                var fi = new FileInfo(f);
                var m = Regex.Match(fi.Name, regex);
                if (m.Success)
                {
                    int room = m.Groups[1].Value.ToIntFromHex();
                    Story story = m.Groups[2].Value[0].ToStory();

                    T o = fi.Name.LoadFile<T>();
                    o.Story = story;
                    o.RoomNumber = room;
                    o.OriginalFileName = fi.Name;
                    o.OriginalFilePath = fi.FullName;

                    yield return o;
                }
            }
        }

        internal static IEnumerable<T> GetWorkFiles<T>(string prefix, Func<string, T> factory) where T : BundleItem
        {
            string regex = $@"^{prefix}_([0-9a-fA-F]+)(c|p|t|x)\.json$";
            var x = Directory.GetFiles(Game.AppDirectory, prefix + "*.json");
            foreach (var f in x)
            {
                var fi = new FileInfo(f);
                var m = Regex.Match(fi.Name, regex);
                if (m.Success)
                {
                    int room = m.Groups[1].Value.ToIntFromHex();
                    Story story = m.Groups[2].Value[0].ToStory();

                    T o = factory(fi.Name);
                    o.Story = story;
                    o.RoomNumber = room;
                    o.OriginalFileName = fi.Name;
                    o.OriginalFilePath = fi.FullName;
                    yield return o;
                }
            }
        }

        public string Store()
        {
            // TODO:  Add integrity checks and compression
            return this.ToJSON();
        }

        public static Bundle Load(string data)
        {
            // TODO:  Add integrity checks and compression
            return data.To<Bundle>();
        }

    }

    public abstract class BundleItem
    {
        public abstract int RoomNumber { get; set; }
        public abstract Story Story { get; set; }
        [JsonIgnore]
        public string OriginalFileName { get; set; }
        [JsonIgnore]
        public string OriginalFilePath { get; set; }
    }
}
