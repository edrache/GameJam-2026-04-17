# Najprostszy serwer high score

To jest celowo najprostsza wersja bez logowania i zabezpieczeń. Nadaje się na game jam albo testy.

## Pliki

Wrzuć folder `server/highscore/` na hosting z PHP.

Serwer będzie miał dwa linki:

```text
https://twoja-domena.pl/highscore/best.php
https://twoja-domena.pl/highscore/save.php?score=123
```

## Jak działa

`best.php` zwraca aktualny najlepszy wynik jako zwykły tekst:

```text
123
```

`save.php?score=456` zapisuje wynik tylko wtedy, gdy jest większy od obecnego rekordu. Odpowiedź też jest zwykłym tekstem i zawsze zwraca najlepszy wynik po próbie zapisu:

```text
456
```

Jeśli wyślesz gorszy wynik, plik nie zostanie zmieniony:

```text
https://twoja-domena.pl/highscore/save.php?score=100
```

Odpowiedź:

```text
456
```

## Ważne

Hosting musi pozwalać PHP zapisać plik `best-score.txt` w tym samym folderze. Jeśli zapis nie działa, utwórz ręcznie plik `best-score.txt`, wpisz do niego `0` i upewnij się, że PHP może go modyfikować.

