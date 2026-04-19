<?php
declare(strict_types=1);

$scoreFile = __DIR__ . '/best-score.txt';
$score = filter_var($_GET['score'] ?? null, FILTER_VALIDATE_INT);

if ($score === false || $score === null || $score < 0) {
    http_response_code(400);
    header('Content-Type: text/plain; charset=utf-8');
    echo 'invalid score';
    exit;
}

if (!file_exists($scoreFile) && file_put_contents($scoreFile, '0', LOCK_EX) === false) {
    http_response_code(500);
    header('Content-Type: text/plain; charset=utf-8');
    echo 'storage error: cannot create best-score.txt';
    exit;
}

$handle = fopen($scoreFile, 'c+');
if ($handle === false) {
    http_response_code(500);
    header('Content-Type: text/plain; charset=utf-8');
    echo 'storage error: best-score.txt is not writable';
    exit;
}

flock($handle, LOCK_EX);
$contents = stream_get_contents($handle);
$bestScore = max(0, (int) trim((string) $contents));

if ($score > $bestScore) {
    ftruncate($handle, 0);
    rewind($handle);
    fwrite($handle, (string) $score);
    $bestScore = $score;
}

fflush($handle);
flock($handle, LOCK_UN);
fclose($handle);

header('Content-Type: text/plain; charset=utf-8');
echo (string) $bestScore;
