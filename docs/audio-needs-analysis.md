# Analiza potrzeb dzwiekowych

Projekt: `GameJam-2026-04-17`  
Data: 2026-04-18  
Zakres: scena `Assets/Scenes/Game.unity`, prefaby gameplayowe w `Assets/Prefabs/`, skrypty w `Assets/TSF/Scripts/` i `Assets/Scripts/`.

## Stan obecny

- W repo jest jeden plik audio: `Assets/TSF/Audio/tsf.aif`.
- `tsf.aif` to stereo AIFF 44.1 kHz, ok. 115.2 s. Jest podpięty jako `AudioSource` w scenie `Assets/TSF/Scenes/stage.unity`, prawdopodobnie jako muzyka/test stage.
- Główna scena `Assets/Scenes/Game.unity` i główne prefaby gameplayowe nie mają jeszcze widocznych `AudioSource` ani wywołań `AudioClip` w kodzie.
- Projekt ma Feel/MMFeedbacks, więc wiele dźwięków warto podpinać jako `MMF_Sound` w istniejących `MMF_Player`, szczególnie tam, gdzie feedback wizualny już istnieje.

## Kierunek brzmienia

Gra jest pierwszoosobowa, stylizowana/toon, z portalami, latarką i minigrami. Dźwięki powinny być czytelne i trochę komiksowe, ale z lekkim kosmicznym napięciem:

- krótkie, sprężyste UI i loot SFX,
- miękkie whoosh/glissando dla portali,
- delikatny, elektryczny hum latarki,
- ambient pokoju/portali jako tło,
- timer z narastającą presją, bez przesadnego hałasu.

## Lista dzwiekow wedlug priorytetu

### P0 - potrzebne do czytelnosci rozgrywki

| ID | Dzwiek | Moment | Miejsce podpiecia | Uwagi |
| --- | --- | --- | --- | --- |
| SFX_FLASHLIGHT_ON | Latarka wlaczona | Gracz naciska akcje `Flashlight` | `Assets/TSF/Scripts/FlashlightController.cs`, metoda `Toggle()` | Krótki klik + miękki zapłon światła. |
| SFX_FLASHLIGHT_OFF | Latarka wylaczona | Gracz naciska akcje `Flashlight` drugi raz | `FlashlightController.Toggle()` | Cichszy klik, bez ogona. |
| SFX_PORTAL_REVEAL_PROGRESS | Portal sie ujawnia | Latarka świeci na portal i rośnie `OpenAmount` | `Assets/TSF/Scripts/PortalAnimator.cs`, `AddExposure()` | Najlepiej loop/warstwa z głośnością zależną od ekspozycji. |
| SFX_PORTAL_OPEN | Portal w pelni otwarty | `_openAmount` dochodzi do 1 | `PortalAnimator.AddExposure()` oraz `Open()` | Jednorazowy shimmer/tonalny akcent. |
| SFX_REACH_START | Reka wchodzi do portalu | Animacja przechodzi w stan `Arm portal` | `Assets/TSF/Scripts/ArmReachController.cs`, `OnHandEnterPortal()` | Ważny feedback, że interakcja weszła. |
| SFX_REACH_EXIT | Reka wychodzi z portalu | Animacja opuszcza portal | `ArmReachController.OnHandExitPortal()` | Krótkie „wyciągnięcie”/suction release. |
| SFX_LOOT_PULL | Loot wyciagniety z portalu | `TriggerLoot(lootPrefab)` i stan animacji `Arm loot` | `ArmReachController.TriggerLoot()` albo `SpawnAndDestroyLoot()` | Może mieć warianty zależne od lootu. |
| SFX_SCORE_GAIN | Punkty dodane | `ScoreManager.AddScore()` | `Assets/TSF/Scripts/ScoreManager.cs` | Krótki pozytywny UI blip; nie za głośny, bo będzie częsty. |
| SFX_TIME_WARNING | Ostatnie sekundy | `ClockTimer` schodzi poniżej `warningTime` | `Assets/Scripts/ClockTimer.cs`, `UpdateColor()` lub `warningFeedback` | Już jest `warningFeedback`, warto dodać `MMF_Sound`. |
| SFX_TIME_UP | Koniec czasu | `ClockTimer.onTimeUp` i `TimesUpWindowController.Show()` | `Assets/Scripts/TimesUpWindowController.cs` / prefab `TimesUpWindowContainer` | Finalny gong lub negatywny stinger. |

### P1 - potrzebne do poczucia jakosci

| ID | Dzwiek | Moment | Miejsce podpiecia | Uwagi |
| --- | --- | --- | --- | --- |
| AMB_ROOM_LOOP | Ambient pokoju | Start sceny gry | `Assets/Scenes/Game.unity` lub nowy `AudioManager` | Lekki room tone + kosmiczne tło, loop. |
| AMB_PORTAL_IDLE_LOOP | Otwarty portal buczy | Portal istnieje / jest otwarty | Prefaby `Assets/Prefabs/Portal.prefab`, `Portal_Boardgames.prefab` | 3D spatial blend, cichy loop przy portalu. |
| SFX_PORTAL_SPAWN | Portal pojawia sie w swiecie | `PortalSpawner.TrySpawnOne()` po `Instantiate` | `Assets/TSF/Scripts/PortalSpawner.cs` | Krótkie „pop-in”; przy 5 portalach nie może być zbyt agresywne. |
| SFX_PORTAL_CLOSE | Portal znika po nagrodzie/porażce | `PortalAnimator.Close()` | `Assets/TSF/Scripts/PortalAnimator.cs` | Powinno pasować do wizualnego zamykania. |
| SFX_SLIDER_START | Start slider mini-gry | `PortalSliderMiniGame.OnHandEnter()` i `PortalTetherSliderMiniGame.OnHandEnter()` | `Assets/TSF/Scripts/PortalSliderMiniGame.cs`, `PortalTetherSliderMiniGame.cs` | Krótki start/interakcja. |
| SFX_SLIDER_FILL_LOOP | Trzymanie interakcji | Slider rośnie do 1 | Obie klasy slider mini-game | Loop z modulacją pitch/volume według `slider.value`. |
| SFX_SLIDER_COMPLETE | Slider dobity | `slider.value >= 1f` | Obie klasy slider mini-game | Pozytywny „complete”. |
| SFX_TETHER_LOCK | Gracz zostaje zablokowany przy portalu | `PortalTetherSliderMiniGame.OnHandEnter()` | `PortalTetherSliderMiniGame` | Dźwięk napięcia/wiązki, bo kamera i dystans są wymuszane. |
| SFX_TETHER_SHAKE | Trzesienie slidera | `sliderShakeFeedbacks` | Prefab `Assets/Prefabs/Portal.prefab` | Tam już jest `MMF_Player`; można dodać sound do feedbacku. |
| SFX_FIND_OPEN | Otwiera sie mini-gra znajdz | `MiniGameFind.OnHandEnter()` | `Assets/Scripts/MiniGameFind.cs` | UI popup / planszówkowe rozłożenie. |
| SFX_FIND_NAV | Przesuniecie fokusu | Strzałki zmieniają `focusedIndex` | `MiniGameFind.HandleInput()` | Krótki tick, najlepiej z wariantami pitch. |
| SFX_FIND_SELECT_CORRECT | Dobry wybór | `Select()` trafia poprawny slot | `MiniGameFind.Select()` | Pozytywny marker. |
| SFX_FIND_SELECT_WRONG | Zły wybór | `ShowWrong()` | `MiniGameFind.ShowWrong()` | Krótki negatywny buzzer; nie za ostry. |
| SFX_FIND_FAIL | Drugi błąd zamyka mini-grę | `wrongCount >= 2` | `MiniGameFind.ShowWrong()` | Mały fail stinger + portal close. |
| SFX_FIND_SUCCESS | Wszystkie 3 poprawne | `MiniGameFind.OnSuccess()` | `MiniGameFind` | Większy sukces niż pojedynczy dobry wybór. |

### P2 - polish i warianty

| ID | Dzwiek | Moment | Miejsce podpiecia | Uwagi |
| --- | --- | --- | --- | --- |
| SFX_FOOTSTEP_WALK | Kroki chodzenia | Gracz się porusza i jest grounded | `Assets/TSF/Scripts/PlayerController.cs` | Brak teraz detekcji kroków; można dodać prosty timer kroków. |
| SFX_FOOTSTEP_SPRINT | Kroki sprintu | Sprint | `PlayerController.Move()` | Szybszy rytm, jaśniejszy transient. |
| SFX_JUMP | Skok | `jumpPressed && isGrounded` | `PlayerController.Move()` | Krótki wysiłek/odbicie. |
| SFX_LAND | Lądowanie | Przejście z airborne na grounded | `PlayerController` po dodaniu stanu poprzedniego grounded | Przydatne, ale wymaga małej rozbudowy. |
| SFX_CAMERA_LOCK | Kamera wymuszona na portal | `SetForcedLookAt()` | `Assets/TSF/Scripts/FPSCameraController.cs` lub mini-gry | Subtelny whoosh, tylko przy wejściu w lock. |
| SFX_HANDS_APPEAR | Ręce/obiekty pojawiają się w portalu | `HandsMiniGame.PlayAppearAnimation()` | `Assets/TSF/Scripts/HandsMiniGame.cs` | Kilka krótkich popów, warianty random. |
| SFX_LOOT_VARIANT_BALLOON | Balon loot | `ballon loot.prefab` | `ArmReachController.TriggerLoot(lootPrefab)` | Gumowy/sprężysty pop. |
| SFX_LOOT_VARIANT_TIRE | Opona loot | `opona loot.prefab` | jw. | Głuchy rubber thump. |
| SFX_LOOT_VARIANT_CUCUMBER | Ogórek loot | `ogorek loot.prefab` | jw. | Mokry, komediowy plop. |
| SFX_LOOT_VARIANT_HOURGLASS | Klepsydra loot | `klepsydra loot.prefab` | jw. | Szkło + piasek. |
| SFX_LOOT_VARIANT_ALIEN | Obcy loot | `obcy loot.prefab` | jw. | Mały sci-fi chirp. |
| SFX_UI_WINDOW_SHOW | Okno końca/okna UI | `TimesUpWindowController.Show()` | `Assets/Prefabs/TimesUpWindowContainer.prefab` | Można podpiąć w istniejącym `showFeedback`. |

## Muzyka i ambience

### Warstwy muzyczne

1. `MUS_GAME_LOOP` - podstawowa pętla gry, 60-120 s, lekka i rytmiczna.
2. `MUS_WARNING_LAYER` - warstwa napięcia albo filtr/automatyka, włącza się przy `ClockTimer.warningTime`.
3. `MUS_TIME_UP_STINGER` - krótki akcent na koniec czasu.

Istniejący `Assets/TSF/Audio/tsf.aif` może być kandydatem na `MUS_GAME_LOOP`, ale trzeba go odsłuchać i zdecydować, czy pasuje do głównej sceny. Technicznie jest gotowy jako stereo 44.1 kHz i trwa ok. 115 s.

### Ambience 3D

- Portal idle: przestrzenny loop na portalu, z rolloffem do kilku metrów.
- Pokój: cichy stereo/2D bed na scenie.
- Latarka: bardzo cichy elektryczny loop tylko gdy włączona, opcjonalnie sidechainowany/ściszony pod ważne SFX.

## Proponowana integracja techniczna

### Najprostszy wariant na jam

- Dodać komponent `AudioSource` na `Player` dla dźwięków 2D/UI/player.
- Dodać `AudioSource` na prefaby portali dla dźwięków 3D (`spatialBlend = 1`).
- W miejscach z `MMF_Player` dodać `MMF_Sound`, bez pisania dodatkowego systemu:
  - `ClockContainer.warningFeedback`,
  - `TimesUpWindowContainer.showFeedback`,
  - `Portal.sliderShakeFeedbacks`.
- W kodzie dodać serializowane `AudioClip` i `AudioSource.PlayOneShot()` tylko tam, gdzie nie ma MMFeedbacks.

### Czystszy wariant

Dodać prosty `GameAudio`/`AudioManager` w `Assets/TSF/Scripts/`, z metodami:

- `Play2D(AudioClip clip, float volume = 1f)`
- `Play3D(AudioClip clip, Vector3 position, float volume = 1f)`
- `SetMusicState(Normal/Warning/TimeUp)`

To ograniczy powielanie `AudioSource` i pozwoli łatwo zrobić losowanie pitch/variantów.

## Konkretny backlog implementacyjny

1. Utworzyć foldery:
   - `Assets/TSF/Audio/Music/`
   - `Assets/TSF/Audio/SFX/`
   - `Assets/TSF/Audio/Ambience/`
2. Dodać prostą strukturę miksera:
   - `Master`
   - `Music`
   - `SFX`
   - `UI`
   - `Ambience`
3. Podpiąć P0:
   - latarka on/off,
   - reach enter/exit,
   - loot pull,
   - score gain,
   - portal open/close,
   - time warning/time up.
4. Podpiąć P1:
   - mini-gry,
   - portal idle/spawn,
   - UI navigation,
   - find correct/wrong/success/fail.
5. Dodać P2:
   - kroki/skok/lądowanie,
   - warianty loot,
   - warstwy muzyki.

## Miejsca ryzyka

- `ScoreManager.AddScore()` może odpalać dźwięk razem z loot SFX; trzeba uważać, żeby sukces nie był przeładowany. Najlepiej: loot pull jest dźwiękiem fizycznym, a score gain bardzo krótki i cichy.
- `PortalSpawner` tworzy kilka portali na starcie, więc `SFX_PORTAL_SPAWN` nie powinien zagrać głośno pięć razy naraz. Można opóźnić spawn sound, zagrać tylko dla portali po starcie albo limitować gęstość.
- `MiniGameFind.cs` ma polskie komentarze/nagłówki, a `AGENTS.md` wymaga angielskiego kodu i komentarzy. Przy okazji implementacji audio warto nie dokładać kolejnych polskich komentarzy w C#.
- Footstep/landing wymagają dodania stanu w `PlayerController`, bo obecnie kod nie śledzi poprzedniego `isGrounded` ani cyklu kroków.

## Minimalny zestaw assetow do zamowienia/stworzenia

Na pierwszą grywalną wersję wystarczy:

- 2x latarka: on/off
- 4x portal: reveal loop, open, idle loop, close
- 3x ręka/loot: reach in, reach out, loot pull
- 4x UI/score/timer: score, warning, time up, window show
- 5x mini-game find: open, nav, correct, wrong, success/fail
- 1x ambient room loop
- 1x music loop

Razem: ok. 20 krótkich plików + 2 loopy. Warianty lootu i footsteps można dodać po tym, gdy podstawowe sprzężenie zwrotne będzie działać.
