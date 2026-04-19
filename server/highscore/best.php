<?php
declare(strict_types=1);

$scoreFile = __DIR__ . '/best-score.txt';

if (!file_exists($scoreFile) && file_put_contents($scoreFile, '0', LOCK_EX) === false) {
    http_response_code(500);
    header('Content-Type: text/plain; charset=utf-8');
    echo 'storage error: cannot create best-score.txt';
    exit;
}

header('Content-Type: text/plain; charset=utf-8');
$score = file_get_contents($scoreFile);
echo trim($score === false ? '0' : $score);
