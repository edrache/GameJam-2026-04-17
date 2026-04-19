<?php
declare(strict_types=1);

$scoreFile = __DIR__ . '/best-score.txt';

if (!file_exists($scoreFile)) {
    file_put_contents($scoreFile, '0', LOCK_EX);
}

header('Content-Type: text/plain; charset=utf-8');
echo trim((string) file_get_contents($scoreFile));

