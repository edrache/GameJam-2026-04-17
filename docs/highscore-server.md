# Najprostszy serwer high score

To jest celowo najprostsza wersja bez logowania i zabezpieczeń. Nadaje się na game jam albo testy.

## Pliki

Wrzuć cały folder `server/` na hosting z PHP.

Serwer będzie miał dwa linki:

```text
https://twoja-domena.pl/highscore/best.php
https://twoja-domena.pl/highscore/save.php?score=123
```

Ma też prostą stronę do ręcznego sprawdzenia:

```text
https://twoja-domena.pl/index.html
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

Hosting musi pozwalać PHP zmieniać plik `highscore/best-score.txt`. Ten plik jest już w projekcie i ma w środku `0`.

Jeśli zapis nie działa i `save.php` zwraca `storage error`, ustaw na hostingu prawo zapisu dla pliku:

```text
highscore/best-score.txt
```

Najczęściej wystarczy w panelu hostingu albo FTP ustawić uprawnienia pliku na `666`. Jeśli hosting wymaga prawa zapisu dla folderu, ustaw też zapis dla folderu `highscore`.

Możesz też otworzyć diagnostykę:

```text
https://twoja-domena.pl/highscore/check.php
```

Jeśli pokaże `File writable: no`, problemem są uprawnienia `best-score.txt`. Jeśli pokaże `File exists: no` i `Folder writable: no`, plik nie został wrzucony albo PHP nie może go utworzyć w folderze `highscore`.
