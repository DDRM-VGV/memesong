using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memesong
{
    
    public class Memesong : Mod, ITogglableMod
    {
        internal static Memesong Instance;
        public override string GetVersion()
        {
            return "1.5.0";
        }
        public List<AudioClip> AreaClips = new List<AudioClip>();
        public List<AudioClip> WinClips = new List<AudioClip>();
        public List<AudioClip> LossClips = new List<AudioClip>();
        public List<AudioClip> InterruptClips = new List<AudioClip>();

        public List<AudioSource> players = new List<AudioSource>();

        public int currentTrack = 0;
        private bool Unloaded = true;

        private Coroutine playRandomlyCoroutine;
        public AudioClip clips { get; private set; }

        public void GetClipsFromDisk(){
            if(!Directory.Exists(Utils.path)){
                return;
            }

            WinClips.AddClipsFromDirectory(Path.Combine(Utils.path,Utils.wins));
            LossClips.AddClipsFromDirectory(Path.Combine(Utils.path,Utils.losses));
            AreaClips.AddClipsFromDirectory(Path.Combine(Utils.path,Utils.area));
            InterruptClips.AddClipsFromDirectory(Path.Combine(Utils.path,Utils.interrupts));

            foreach(var clip in InterruptClips){
                WinClips.Add(clip);
                LossClips.Add(clip);
            }

        }

        public override void Initialize()
        {
            if(!Unloaded) { return; }
            Log("Initializing Memesong");
            Instance = this;

            AreaClips = new List<AudioClip>();
            WinClips = new List<AudioClip>();
            LossClips = new List<AudioClip>();
            InterruptClips = new List<AudioClip>();
            Utils.ExtractAudioFiles();
            GetClipsFromDisk();
            ModHooks.Instance.HeroUpdateHook += update;
            ModHooks.Instance.OnRecieveDeathEventHook += EnemyDied;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneChange;
            ModHooks.Instance.BeforePlayerDeadHook += OnDeath;
            ModHooks.Instance.TakeHealthHook += TakeDamage;

            if (players.Count < 5)
            {
                poolPlayers(5);
            }
            playRandomlyCoroutine = GameManager.instance.StartCoroutine(playRandomly());

            Unloaded = false;
            Debug.Log("Done initializing Memesong");
        }

        public void poolPlayers(int count){
            for(var i =0 ; i < count ; i++){
                var go = new GameObject("audio player ");
                var asrc = go.AddComponent<AudioSource>();
                players.Add(asrc);
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            Log("Total players : "+ players.Count);
        }
        public AudioSource getPlayerFromPool(){
           AudioSource asrc = null;
           foreach(var p in players){
               if(!p.isPlaying){
                asrc = p;
                break;
               }
           }
           return asrc;
        }
        public IEnumerator playRandomly(){
            var chance = 0.05;
            while(true){
                yield return new WaitForSeconds(30);
                if(chance * 100 >= Utils.random.Next(100) && !Unloaded){
                    //should play
                    play(InterruptClips[Utils.random.Next(InterruptClips.Count)]);
                };
            }
        }
        public void play(AudioClip clip){
           AudioSource asrc = getPlayerFromPool();
           if(asrc == null){
               poolPlayers(5);
                asrc = getPlayerFromPool();
           }
           asrc.PlayOneShot(clip,GameManager.instance.GetImplicitCinematicVolume());
        }

        public AudioSource scenePlayer;
        public void playForScene(AudioClip clip,bool forced = false){
            if(scenePlayer == null){
                var go = new GameObject("scene audio player");
                scenePlayer = go.AddComponent<AudioSource>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            if(forced){
                scenePlayer.Stop();
            }
            if(!scenePlayer.isPlaying) {
                if (Unloaded) return;
                scenePlayer.PlayOneShot(clip,GameManager.instance.GetImplicitCinematicVolume());
            }
        }


        public bool EnemyDied( EnemyDeathEffects enemyDeathEffects, bool eventAlreadyRecieved,ref float? attackDirection, ref bool resetDeathEvent,ref bool spellBurn, ref bool isWatery ){
            if(!eventAlreadyRecieved){
                if (!Unloaded){
                    play(WinClips[Utils.random.Next(WinClips.Count)]);
                }
            }
            return true;
        }
        public void SceneChange(Scene scene,LoadSceneMode mode){
            if (Unloaded) return;
            var roll = Utils.random.Next(100);            
            if(70 >= roll){
                playForScene(AreaClips[Utils.random.Next(AreaClips.Count)]);
            }
        }
        public void OnDeath() {
            if (Unloaded) return;
            playForScene(LossClips[Utils.random.Next(LossClips.Count)],true);
        }
        public int TakeDamage( int damage ){
            if (Unloaded) return damage;
            var roll = Utils.random.Next(100);
            if (10 >= roll){
                play(InterruptClips[Utils.random.Next(InterruptClips.Count)]);
            } else if(15 >= roll){
                play(LossClips[Utils.random.Next(LossClips.Count)]);
            } else if(16 >= roll){
                play(WinClips[Utils.random.Next(WinClips.Count)]);
            }
            return damage;
        }

        public void update()
        {
            // if needed
            if(false && Input.GetKeyDown(KeyCode.C) && !Unloaded){
                if(currentTrack == WinClips.Count){
                    currentTrack = 0;
                } else {
                    currentTrack+=1;
                }
                play(WinClips[currentTrack]);
            }
        }

        public static void cleanList<T>(List<T> list) where T : UnityEngine.Object{
            foreach (T item in list){
                UnityEngine.Object.Destroy(item);
            }
            list.Clear();
        }
        public static void cleanList(List<Component> list){
            foreach (Component item in list){
                if(item.gameObject == null){
                    UnityEngine.Object.Destroy(item);
                } else {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }
            list.Clear();
        }
        public void Unload()
        {
            Debug.Log("Unloading Memesong");

            if(playRandomlyCoroutine != null){
                GameManager.instance.StopCoroutine(playRandomlyCoroutine);
            }

            cleanList<AudioClip>(WinClips);
            cleanList<AudioClip>(AreaClips);
            cleanList<AudioClip>(LossClips);
            cleanList<AudioClip>(InterruptClips);
            cleanList<AudioClip>(InterruptClips);
            cleanList<AudioSource>(players);

            ModHooks.Instance.HeroUpdateHook -= update;
            ModHooks.Instance.OnRecieveDeathEventHook -= EnemyDied;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneChange;
            ModHooks.Instance.BeforePlayerDeadHook -= OnDeath;
            ModHooks.Instance.TakeHealthHook -= TakeDamage;

            Instance = null;
            Unloaded = true;
            Debug.Log("Done unloading Memesong");
        }
    }
}
