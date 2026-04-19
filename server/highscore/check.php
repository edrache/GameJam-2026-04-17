<?php
declare(strict_types=1);

$scoreFile = __DIR__ . '/best-score.txt';

header('Content-Type: text/plain; charset=utf-8');

echo "High score storage check\n";
echo "========================\n";
echo "Folder: " . __DIR__ . "\n";
echo "Score file: " . $scoreFile . "\n";
echo "File exists: " . (file_exists($scoreFile) ? 'yes' : 'no') . "\n";
echo "File readable: " . (is_readable($scoreFile) ? 'yes' : 'no') . "\n";
echo "File writable: " . (is_writable($scoreFile) ? 'yes' : 'no') . "\n";
echo "Folder writable: " . (is_writable(__DIR__) ? 'yes' : 'no') . "\n";

if (!file_exists($scoreFile)) {
    echo "\nTrying to create best-score.txt...\n";
    $created = file_put_contents($scoreFile, '0', LOCK_EX);
    echo "Create result: " . ($created === false ? 'failed' : 'ok') . "\n";
    echo "File exists now: " . (file_exists($scoreFile) ? 'yes' : 'no') . "\n";
    echo "File writable now: " . (is_writable($scoreFile) ? 'yes' : 'no') . "\n";
}

if (file_exists($scoreFile)) {
    echo "\nCurrent value: " . trim((string) file_get_contents($scoreFile)) . "\n";
}

