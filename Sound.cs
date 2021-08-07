using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX
{
    /// <summary>
    /// Represents a Sound asset
    /// </summary>
    public class Sound
    {
        /// <summary>
        /// List of all Sound assets
        /// </summary>
        public static List<Sound> Sounds = new List<Sound>();

        /// <summary>
        /// Reference to the Game
        /// </summary>
        public static Game Game;

        /// <summary>
        /// The SoundEffect object that represents the sound asset
        /// </summary>
        public SoundEffect SoundEffect;

        /// <summary>
        /// The SoundEffectInstance for this sound
        /// </summary>
        public SoundEffectInstance Instance;

        /// <summary>
        /// List of sounds that need to be actively checked for finishing
        /// </summary>
        public static SafeList<Sound> CheckedSounds = new List<Sound>();

        /// <summary>
        /// Flags pertaining to this Sound
        /// </summary>
        public SoundFlags Flags { get; set; }

        /// <summary>
        /// Returns true if this Sound is playing
        /// </summary>
        public virtual bool IsPlaying => Instance.State == SoundState.Playing;

        /// <summary>
        /// Tracks whether a sound *was* playing -- if WasPlaying is true but IsPlaying is false, 
        /// the sound most likely finished normally
        /// </summary>
        public virtual bool WasPlaying { get; protected set; }
        /// <summary>
        /// Volume for sounds with the Sound (SFX) flag
        /// </summary>
        public static double SoundVolume { get; set; } = 1.0;
        /// <summary>
        /// Volume for sounds with the Music flag
        /// </summary>
        public static double MusicVolume { get; set; } = 1.0;

        // List of sound asset references
        public static Sound ExtraLife;
        public static Sound Break;
        public static Sound Burn;
        public static Sound Collect;
        public static Sound Die;
        public static Sound Door;
        public static Sound Fairy;
        public static Sound Fire;
        public static Sound GameOver;
        public static Sound Head;
        public static Sound Hiss;
        public static Sound Key;
        public static Sound Make;
        public static Sound Pause;
        public static Sound Reveal;
        public static Sound Rumble;
        public static Sound Rumble2;
        public static Sound Shoot;
        public static Sound Start;
        public static Sound ThankYou;
        public static Sound Tick;
        public static Sound Warp;
        public static Sound Wince;
        // Add sounds here

        // Multi-track Music
        public static MultiTrack Intro;
        public static MultiTrack Music;
        public static MultiTrack HiddenIntro;
        public static MultiTrack HiddenMusic;
        public static MultiTrack LowTime;
        public static MultiTrack Vapour;
        public static MultiTrack Ending;
        public static MultiTrack BB;
        public static MultiTrack Bonzai;
        public static MultiTrack BBLow, BBLow2;
        public static MultiTrack MillIntro, Mill1;
        // Add music here

        // Used to handle sound interruptions (like the Key sound effect)
        protected static Stack<Interruption> Interruption = new Stack<Interruption>();


        // String used in the sound test and other places
        public virtual string Name => SoundEffect?.Name ?? "?";

        // Used to track whether or not we've paused/un-paused all the sounds during Game-level pause
        private static bool paused;
        // Local cache of the Game.Fade
        private static float fade;


        // Constructor
        public Sound(string name, SoundFlags flags)
        {
            if (name != null)
            {
                var sfx = Game.Content.Load<SoundEffect>(name);
                if (sfx != null)
                {
                    Flags = flags;
                    SoundEffect = sfx;
                    Instance = sfx.CreateInstance();
                    Instance.IsLooped = flags.HasFlag(SoundFlags.Loop);
                    Sounds.Add(this);
                }
            }
        }

        // Constructor used by MultiTrack
        internal Sound(bool multi, SoundEffect sfx, SoundFlags flags)
        {
            Flags = flags;
            SoundEffect = sfx;
            Instance = sfx.CreateInstance();
            Instance.IsLooped = flags.HasFlag(SoundFlags.Loop);
        }

        /// <summary>
        /// Updates music and sound effect volumes
        /// </summary>
        public static void UpdateVolumes()
        {
            foreach(var s in Sounds)
            {
                if (s.Flags.HasFlag(SoundFlags.Music))
                {
                    s.Volume(MusicVolume * fade);
                }
                else if (s.Flags.HasFlag(SoundFlags.Sound))
                {
                    s.Volume(SoundVolume);
                }
            }
        }

        /// <summary>
        /// Sets the volume for this Sound
        /// </summary>
        public virtual void Volume(double vol)
        {
            if (Instance is null) return;
            if (vol < 0) vol = 0;
            if (vol > 1) vol = 1;
            Instance.Volume = (float)vol;
        }

        /// <summary>
        /// Plays the sound, cutting off the sound and restarting it if it's already
        /// playing, unless it has the NoRestart flag set
        /// </summary>
        public virtual void Play()
        {
            if (!Flags.HasFlag(SoundFlags.NoRestart) && Instance.State == SoundState.Playing) Stop();
            WasPlaying = true;
            Instance.Play();
        }

        /// <summary>
        /// Resumes a sound from where it was paused
        /// </summary>
        public virtual void Resume()
        {
            WasPlaying = true;
            Instance.Resume();
        }

        /// <summary>
        /// Interrupts all other sounds with this sound
        /// until it finishes
        /// </summary>
        public virtual void Interrupt()
        {
            var l = new List<Sound>();
            foreach(var s in Sounds)
            {
                if (s.IsPlaying)
                {
                    l.Add(s);
                    s.Interrupted();
                }
            }
            Interruption.Push(new Interruption(l.ToArray(), this));
            Play();
        }

        /// <summary>
        /// Used to pause a sound when an interrupting sound is played
        /// </summary>
        public virtual void Interrupted()
        {
            if (IsPaused || !IsPlaying) return;
            Instance.Pause();
        }

        /// <summary>
        /// Returns true if the sound is paused
        /// </summary>
        public virtual bool IsPaused => Instance.State == SoundState.Paused;

        /// <summary>
        /// Called every game tick we should be paused
        /// </summary>
        public static void PauseSound()
        {
            if (paused) return;
            paused = true;
            foreach(var s in Sounds)
            {
                if (s == Pause) continue;   // Don't pause the pause sound
                if (s.IsPlaying)
                    s.Stop(true);   // Pause the sound
            }
        }

        /// <summary>
        /// Used by derived classes that need to update things
        /// </summary>
        public virtual void Update() 
        {
            if (fade != Game.Fade)
            {
                fade = Game.Fade;
                UpdateVolumes();
            }
        }

        /// <summary>
        /// Called every game tick we should be playing music
        /// </summary>
        public static void GoSound()
        {
            // Check to see if intro is done
            foreach(var s in CheckedSounds)
            {
                s.Update();
            }

            // Interruption check
            if (Interruption.Count > 0)
            {
                var i = Interruption.Peek();
                if (i.InterruptingSound != null && !i.InterruptingSound.IsPlaying)
                {
                    Interruption.Pop();
                    foreach (var ii in i.InterruptedSounds)
                    {
                        if (ii.WasPlaying) ii.Resume();
                    }
                }
            }
            
            // See if we need to unpause all the sounds because
            // the game was just unpaused
            if (!paused) return;
            paused = false;
            foreach (var s in Sounds)
            {
                if (s.IsPaused)
                {
                    s.Resume();
                }
            }
        }

        /// <summary>
        /// Stops the sound if it's playing
        /// </summary>
        public virtual void Stop(bool pause = false)
        {
            if (pause)
            {
                if (IsPaused || !IsPlaying) return;
                Instance.Pause();
                return;
            }

            // This is here to fix a bug in MonoGame
            if (IsPaused)
            {
                // If you stop a paused SFX you'll never ever be able to pause it again
                Instance.Resume();
            }

            WasPlaying = false;
            Instance.Stop();
        }

        /// <summary>
        /// Stops all sounds from playing
        /// </summary>
        public static void StopAll()
        {
            foreach (var s in Sounds)
            {
                s.Stop();
            }

        }

      
        /// <summary>
        /// Loads all the Sound Assets from the content pipeline.
        /// Honestly I have no idea why people bitch about the content pipeline being difficult to use. ~nill
        /// </summary>
        public static void LoadSounds(Game game)
        {
            Game = game;

            Intro = new MultiTrack("intro", SoundFlags.Music, "i1", "i2", "i3");
            Music = new MultiTrack("music", SoundFlags.Music | SoundFlags.Loop, "m1", "m2", "m3");
            Intro.Next = Music;

            LowTime = new MultiTrack("lowtime", SoundFlags.Music | SoundFlags.Loop, "l1", "l2", "l3");
            Vapour = new MultiTrack("vapour", SoundFlags.Music | SoundFlags.Loop, "v1");
            Ending = new MultiTrack("ending", SoundFlags.Music | SoundFlags.Loop, "e0");
            BB = new MultiTrack("bbm", SoundFlags.Music | SoundFlags.Loop, "bb");
            BBLow = new MultiTrack("bbli", SoundFlags.Music, "bbl1");
            BBLow2 = new MultiTrack("bblm", SoundFlags.Music, "bbl2");
            BBLow.Next = BBLow2;

            HiddenIntro = new MultiTrack("hintro", SoundFlags.Music, "hi1", "hi2");
            HiddenMusic = new MultiTrack("hmusic", SoundFlags.Music | SoundFlags.Loop, "h1", "h2");
            HiddenIntro.Next = HiddenMusic;

            MillIntro = new MultiTrack("mintro", SoundFlags.Music, "milli");
            Mill1 = new MultiTrack("mmusic", SoundFlags.Music | SoundFlags.Loop, "mill1");
            MillIntro.Next = Mill1;

            GameOver = new Sound("gameover", SoundFlags.Music);
            Bonzai = new MultiTrack("bonzai", SoundFlags.Music | SoundFlags.Loop, "bonz");
            ExtraLife = new Sound("1up", SoundFlags.Music);
            ThankYou = new Sound("thankyou", SoundFlags.Music | SoundFlags.Loop);

            Break = new Sound("break", SoundFlags.Sound);
            Burn = new Sound("burn", SoundFlags.Sound);
            Collect = new Sound("collect", SoundFlags.Sound);
            Die = new Sound("die", SoundFlags.Sound);
            Fairy = new Sound("fairy", SoundFlags.Sound);
            Door = new Sound("door", SoundFlags.Music);
            Fire = new Sound("fire", SoundFlags.Sound);
            Head = new Sound("head", SoundFlags.Sound);
            Hiss = new Sound("hiss", SoundFlags.Sound);
            Key = new Sound("key", SoundFlags.Sound);
            Make = new Sound("make", SoundFlags.Sound);
            Pause = new Sound("pause", SoundFlags.Sound);
            Rumble = new Sound("rumble", SoundFlags.Sound);
            Rumble2 = new Sound("rumble2", SoundFlags.Sound);
            Shoot = new Sound("shoot", SoundFlags.Sound);
            Start = new Sound("start", SoundFlags.Sound);
            Warp = new Sound("warp", SoundFlags.Sound);
            Wince = new Sound("wince", SoundFlags.Sound);
            Tick = new Sound("tick", SoundFlags.Sound);
            Reveal = new Sound("reveal", SoundFlags.Sound);

            UpdateVolumes();
        }

        /// <summary>
        /// Emits the status of the sound as a string
        /// </summary>
        public override string ToString()
        {
            var p = IsPlaying ? "P" : "-";
            var u = IsPaused ? "U" : "-";
            var w = WasPlaying ? "W" : "-";
            return $"{Name.ToUpper(),10} {p}{u}{w}";
        }

    }

    /// <summary>
    /// A MultiTrack is a specialty of Sound that provides the following two added benefits:
    /// 1.  Plays multiple simultaneous tracks in sync
    /// 2.  Can "chain" playback to another Sound when it reaches the end
    /// 
    /// If a MultiTrack is chained to another sound,  once the MultiTrack finishes playing
    /// the next sound will automatically play,  and any command on this object (Play, Stop, etc.)
    /// will pass through to the sound currently playing;  thus providing both sequential and/or parallel
    /// control over multiple sounds in a single Sound object.
    /// </summary>
    public class MultiTrack : Sound
    {
        /// <summary>
        /// The individual tracks that make up this multi-sound
        /// </summary>
        public List<Track> Tracks { get; }
        /// <summary>
        /// The sound (if any) to play when this one finishes
        /// </summary>
        public MultiTrack Next { get; set; }
        /// <summary>
        /// Tracks whether or not the current sound has finished,  if so,
        /// commands (Stop, IsPlaying, WasPlaying, Interrupted, etc.) will
        /// pass through to the Next sound
        /// </summary>
        public bool Finished { get; private set; }
        /// <summary>
        /// The name of the MultiTrack
        /// </summary>
        public override string Name { get; }
        /// <summary>
        /// Master volume of the MultiTrack
        /// </summary>
        public double MasterVolume { get; set; } = 1.0;
        /// <summary>
        /// Master pan of the MultiTrack
        /// </summary>
        public double MasterPan{ get; set; } = 0.0;

        /// <summary>
        /// Dynamic panning and fading delegates
        /// </summary>
        public MixPlan MixPlan;

        // Internals
        private bool wasplaying;
        // How many ticks since we last fired MixPlan delegates
        private uint MixTicks;
        // Used for ToString()
        private StringBuilder buffer = new StringBuilder(50);

        /// <summary>
        /// Used to determine if the sound was playing (and then may have stopped)
        /// </summary>
        public override bool WasPlaying
        {
            get => Finished ? Next.WasPlaying : wasplaying;
            protected set { if (Finished) Next.WasPlaying = value; else wasplaying = value; }
        }

        /// <summary>
        /// Creates a new MultiTrack
        /// </summary>
        /// <param name="name">The name of the MultiTrack</param>
        /// <param name="flags">Flags for the sound (and its children tracks)</param>
        /// <param name="soundNames">The names of the assets that make up the tracks for this sound</param>
        public MultiTrack(string name, SoundFlags flags, params string[] soundNames) : base(null, flags)
        {
            Name = name;
            Flags = flags;
            Tracks = new List<Track>();
            foreach(var s in soundNames)
            {
                var ss = new Sound(true, Game.Content.Load<SoundEffect>(s), flags);
                ss.Instance.IsLooped = flags.HasFlag(SoundFlags.Loop);
                var t = new Track(ss, this);
                Tracks.Add(t);
            }
            Sounds.Add(this);
        }

        /// <summary>
        /// Sets the volume for all tracks at once
        /// </summary>
        public override void Volume(double vol)
        {
            MasterVolume = vol;
            foreach(var t in Tracks)
            {
                t.UpdateVolume();
            }
        }

        /// <summary>
        /// Updates all mix tracks according to the MixPlan
        /// </summary>
        void MixUpdate()
        {
            MixTicks++;
            if (MixPlan.SampleFrequency < 4) MixPlan.SampleFrequency = 4;
            if (MixTicks > MixPlan.SampleFrequency)
            {
                MixTicks = 0;
                MixFire();
            }
            
        }

        /// <summary>
        /// Fire delegates to update fade/pan of tracks
        /// </summary>
        void MixFire()
        {

            if (MixPlan.Track0Fade != null && Tracks.Count > 0)
            {
                Tracks[0].Volume = MixPlan.Track0Fade();
                Tracks[0].UpdateVolume();
            }
            if (MixPlan.Track0Pan != null && Tracks.Count > 0)
            {
                Tracks[0].Pan = MixPlan.Track0Pan();
                Tracks[0].UpdatePan();
            }
            if (MixPlan.Track1Fade != null && Tracks.Count > 1)
            {
                Tracks[1].Volume = MixPlan.Track1Fade();
                Tracks[1].UpdateVolume();

            }
            if (MixPlan.Track1Pan != null && Tracks.Count > 1)
            {
                Tracks[1].Pan = MixPlan.Track1Pan();
                Tracks[1].UpdatePan();
            }
            if (MixPlan.Track2Fade != null && Tracks.Count > 2)
            {
                Tracks[2].Volume = MixPlan.Track2Fade();
                Tracks[2].UpdateVolume();

            }
            if (MixPlan.Track2Pan != null && Tracks.Count > 2)
            {
                Tracks[2].Pan = MixPlan.Track2Pan();
                Tracks[2].UpdatePan();
            }
        }

        /// <summary>
        /// Resets pan and fade on all tracks
        /// </summary>
        public void ResetMix()
        {
            foreach(var t in Tracks)
            {
                t.Volume = 1.0f;
                t.Pan = 0.0f;
                t.UpdateVolume();
            }
            if (Next != null) Next.ResetMix();
        }

        /// <summary>
        /// Checks if the song has ended and the next one should begin
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (!WasPlaying) return;
            if (Interruption.Count > 0) return;

            MixUpdate();

            if (!IsPlaying)
            {
                if (Next != null)
                {
                    Finished = true;
                    Next.MixPlan = MixPlan;
                    Next.MixFire();
                    Next.Play();
                } else
                {
                    // The chain has ended
                    wasplaying = false;
                }
            }
        }

        /// <summary>
        /// Determines if the mix (or a chained mix) is currently paused
        /// </summary>
        public override bool IsPaused => Finished ? Next.IsPaused : Tracks[0].Sound.IsPaused;

        /// <summary>
        /// Determines if the mix (or a chained mix) is currently playing
        /// </summary>
        public override bool IsPlaying => Finished ? Next.IsPlaying : Tracks[0].Sound.IsPlaying;

        /// <summary>
        /// Plays the mix
        /// </summary>
        public override void Play()
        {
            if (Finished)
            {
                Next.Play();
                return;
            }

            if (!CheckedSounds.Contains(this))
            {
                CheckedSounds.Add(this);
            }

            wasplaying = true;
            foreach (var t in Tracks)
            {
                t.Sound.Play();
            }
        }

        /// <summary>
        /// Resumes a paused mix
        /// </summary>
        public override void Resume()
        {
            wasplaying = true;

            if (Finished)
            {
                Next.Resume();
                return;
            }

            foreach (var t in Tracks)
            {
                t.Sound.Resume();
            }
        }

        /// <summary>
        /// Called when an interrupting sound takes over
        /// </summary>
        public override void Interrupted()
        {
          
            if (Finished)
            {
                Next.Interrupted();
                return;
            }

            foreach(var t in Tracks)
            {
                t.Sound.Interrupted();
            }
        }

        /// <summary>
        /// Stops the mix
        /// </summary>
        /// <param name="pause"></param>
        public override void Stop(bool pause = false)
        {

            wasplaying = false;

            if (Finished)
            {
                Next.Stop(pause);
                if (!pause)
                {
                    Finished = false;
                }
                return;
            }

            foreach (var t in Tracks)
            {
                t.Sound.Stop(pause);
            }
        }

        /// <summary>
        /// Returns a string representing the mix status
        /// </summary>
        public override string ToString()
        {
            var p = IsPlaying ? "P" : "-";
            var u = IsPaused ? "U" : "-";
            var w = WasPlaying ? "W" : "-";
            var f = Finished ? "F" : "-";
            var a = wasplaying ? "A" : "-";
            buffer.Clear();
            foreach(var t in Tracks)
            {
                buffer.Append($"{t.Volume * 10:00}{t.Pan * 10:00} ");
            }
            return $"{Name.ToUpper(),10} {p}{u}{w}{f}{a} {buffer}";
        }


    }

    /// <summary>
    /// Represents a track in a MultiTrack sound
    /// </summary>
    public class Track
    {
        /// <summary>
        /// The sound played on this track
        /// </summary>
        public Sound Sound { get; }
        /// <summary>
        /// The parent mix
        /// </summary>
        public MultiTrack Parent { get; }
        /// <summary>
        /// Volume/fade for this track
        /// </summary>
        public double Volume { get; set; } = 1.0;
        /// <summary>
        /// Pan for this track
        /// </summary>
        public double Pan { get; set; } = 0.0;

        /// <summary>
        /// Creates a new track
        /// </summary>
        public Track(Sound sound, MultiTrack parent)
        {
            Sound = sound;
            Parent = parent;
        }

        /// <summary>
        /// Updates the inner sound's volume to be the track volume * the multi-track master volume
        /// </summary>
        public void UpdateVolume()
        {
            Sound.Volume((float)(Volume * Parent.MasterVolume));
        }

        /// <summary>
        /// Updates the inner sound's pan to be the track pan * the multi-track master pan
        /// </summary>
        public void UpdatePan()
        {
            Sound.Instance.Pan = (float)(Pan * Parent.MasterPan);
        }
    }

    /// <summary>
    /// Flags for Sounds
    /// </summary>
    [Flags]
    public enum SoundFlags
    {
        None,           // Default value
        Music = 1,      // Use music volume 
        Sound = 2,      // Use SFX volume
        Loop = 4,       // Loop at end
        NoRestart = 8   // Don't restart if played twice
    }

    /// <summary>
    /// A mix plan tells us how to pan and fade the individual tracks
    /// based on game inputs
    /// </summary>
    public struct MixPlan
    {
        /// <summary>How often the lambdas should be executed</summary>
        public uint SampleFrequency;
        /// <summary>Track 0 Volume</summary>
        public Func<float> Track0Fade;
        /// <summary>Track 0 Pan</summary>
        public Func<float> Track0Pan;
        /// <summary>Track 1 Volume</summary>
        public Func<float> Track1Fade;
        /// <summary>Track 1 Pan</summary>
        public Func<float> Track1Pan;
        /// <summary>Track 2 Volume</summary>
        public Func<float> Track2Fade;
        /// <summary>Track 2 Pan</summary>
        public Func<float> Track2Pan;

    }

    /// <summary>
    /// Represents an interruption event when one sound interrupts all others.
    /// These are stored in a stack to avoid race conditions and to ensure interrupted
    /// sounds (which become paused) resume playing properly
    /// </summary>
    public class Interruption
    {
        public Sound InterruptingSound { get; }
        public Sound[] InterruptedSounds { get; }

        public Interruption(Sound[] interruptedSounds, Sound interrupedBy)
        {
            InterruptedSounds = interruptedSounds;
            InterruptingSound = interrupedBy;
        }
    }
}
