<?php
error_reporting(E_ALL);
ini_set('display_errors', 1);
@set_time_limit(600);
@ini_set('max_execution_time', 600);

register_shutdown_function(function () {
    $error = error_get_last();
    if ($error && ($error['type'] === E_ERROR || $error['type'] === E_PARSE || $error['type'] === E_COMPILE_ERROR)) {
        ob_clean();
        echo json_encode(['status' => 'error', 'message' => 'PHP Fatal Error: ' . $error['message'], 'file' => $error['file'], 'line' => $error['line']]);
        exit;
    }
});

/**
 * RamboWoon Deployment Bridge [v6.3 Platinum Lite]
 * Precision Deployment & Atomic Security
 */
class RamboWoonBridge
{
    private $version = "6.3 Platinum Lite";
    private $action;

    public function __construct()
    {
        $this->action = $_GET['action'] ?? '';
    }

    public function run()
    {
        switch ($this->action) {
            case 'ping':
                $this->ping();
                break;
            case 'deploy':
                $this->deploy();
                break;
            case 'deployDb':
                $this->deployDb();
                break;
            case 'checkDb':
                $this->checkDb();
                break;
            case 'cleanup':
                $this->cleanup();
                break;
            default:
                echo json_encode(['status' => 'error', 'message' => 'Invalid action']);
                break;
        }
    }

    private function ping()
    {
        header('Content-Type: application/json');
        die(json_encode(['status' => 'success', 'message' => 'pong', 'version' => $this->version, 'php' => PHP_VERSION]));
    }

    public function deploy($previousResults = [])
    {
        $results = is_array($previousResults) ? $previousResults : [];
        $zipFile = 'dist.zip';
        $sqlFile = 'dist.sql';

        $baseDir = __DIR__;
        $results[] = "Bridge Work Dir: " . $baseDir;

        // Multi-level file resolution (current, parent, grandparent)
        $searchPaths = ['.', '..', '../..'];
        foreach ($searchPaths as $path) {
            if (!file_exists($sqlFile) && file_exists($path . '/dist.sql')) {
                $sqlFile = $path . '/dist.sql';
                $results[] = "SQL found in: $sqlFile";
            }
            if (!file_exists($zipFile) && file_exists($path . '/dist.zip')) {
                $zipFile = $path . '/dist.zip';
                $results[] = "ZIP found in: $zipFile";
            }
        }

        $isSqlSuccess = false;
        $isZipSuccess = false;

        // Robust data parsing (v6.3 Ultra Recov)
        $dbRaw = $_POST['db_config'] ?? null;
        $appRaw = $_POST['app_config'] ?? null;

        if (!$dbRaw) {
            $rawInput = file_get_contents('php://input');
            if (!empty($rawInput)) {
                parse_str($rawInput, $inputData);
                $dbRaw = $inputData['db_config'] ?? null;
                $appRaw = $inputData['app_config'] ?? null;
            }
        }

        $dbConfig = json_decode($dbRaw, true);
        $appConfig = json_decode($appRaw, true);

        if (empty($dbConfig) || empty($dbConfig['name'])) {
            $dbConfig = $this->getDbConfigFromEnv();
            $results[] = "Loaded database config from server .env (Host=" . ($dbConfig['host'] ?? 'localhost') . ", DB=" . ($dbConfig['name'] ?? 'none') . ")";
        }

        $results = array_merge($results, $previousResults);
        $results[] = "--- Starting Deploy [Bridge {$this->version}] ---";

        // Backup existing .htaccess if skip runs
        $htaccessBackup = null;
        if (!empty($_POST['skip_htaccess']) && file_exists('.htaccess')) {
            $htaccessBackup = file_get_contents('.htaccess');
            $results[] = "Backed up local .htaccess";
        }

        // Backup existing .env to preserve server-side settings
        $envBackup = null;
        if (file_exists('.env')) {
            $envBackup = file_get_contents('.env');
            $results[] = "Backed up existing .env configuration";
        }

        // Clean up any old files with backslashes in their names from previous buggy deploys
        try {
            if (class_exists('DirectoryIterator')) {
                $dir = new DirectoryIterator($baseDir);
                foreach ($dir as $fileinfo) {
                    if (!$fileinfo->isDot() && !$fileinfo->isDir()) {
                        $filename = $fileinfo->getFilename();
                        if (strpos($filename, '\\') !== false) {
                            @unlink($baseDir . '/' . $filename);
                        }
                    }
                }
            }
        } catch (\Exception $e) {
            // Ignore
        }

        // 1. Unzip - Use System Unzip if possible (Fast & avoids 500)
        if (file_exists($zipFile)) {
            $results[] = "Extracting package (" . filesize($zipFile) . " bytes)...";
            $unzipped = false;

            // Read zip metadata for progress reporting
            $totalFiles = 0;
            $uncompressedSize = 0;
            if (class_exists('ZipArchive')) {
                $zipMeta = new ZipArchive();
                if ($zipMeta->open($zipFile) === TRUE) {
                    $totalFiles = $zipMeta->numFiles;
                    for ($i = 0; $i < $totalFiles; $i++) {
                        $stat = $zipMeta->statIndex($i);
                        $uncompressedSize += $stat['size'];
                    }
                    $zipMeta->close();
                }
            }

            // Method 1: System Unzip (Fastest)
            $unzipped = false;
            if (function_exists('exec')) {
                @exec("unzip -o $zipFile -d .", $output, $returnVar);
                if (isset($returnVar) && $returnVar === 0) {
                    $unzipped = true;
                    $extractedFiles = is_array($output) ? count($output) : $totalFiles;
                    $results[] = "Package extracted via system unzip: $extractedFiles entries. Total size: " . round($uncompressedSize / 1024 / 1024, 2) . " MB";
                }
            }

            if (!$unzipped) {
                // Method 2: PHP Fallback
                if (class_exists('ZipArchive')) {
                    $zip = new ZipArchive();
                    if ($zip->open($zipFile) === TRUE) {
                        if ($zip->extractTo('.')) {
                            $unzipped = true;
                            $results[] = "Package extracted via PHP ZipArchive: $totalFiles files. Total size: " . round($uncompressedSize / 1024 / 1024, 2) . " MB";
                        }
                        $zip->close();
                    }
                }
            }

            if ($unzipped) {
                $isZipSuccess = true;

                // Clear Laravel bootstrap config/routes cache if present
                $cacheFiles = [
                    'bootstrap/cache/config.php',
                    'bootstrap/cache/routes.php',
                    'bootstrap/cache/events.php'
                ];
                foreach ($cacheFiles as $cf) {
                    if (file_exists($cf)) {
                        @unlink($cf);
                        $results[] = "Cleared Laravel cache file: $cf";
                    }
                }
            } else {
                die(json_encode(['status' => 'error', 'message' => 'Lỗi: Không thể giải nén bộ cài (Cả unzip và ZipArchive đều thất bại hoặc treo).', 'logs' => $results]));
            }
        } else {
            $results[] = "ZIP package NOT found on server";
        }

        // Restore .env after unzip (so we don't lose server-specific keys)
        if ($envBackup !== null) {
            file_put_contents('.env', $envBackup);
            $results[] = "Restored existing .env configuration";
        }

        // Restore existing .htaccess
        if ($htaccessBackup !== null) {
            file_put_contents('.htaccess', $htaccessBackup);
            $results[] = "Restored local .htaccess";
        } else if (!empty($_POST['skip_htaccess']) && file_exists('.htaccess')) {
            // Unzipped Demo's htaccess on a fresh Prod instance. Strip Demo's SSL block to prevent redirect loops.
            $content = file_get_contents('.htaccess');
            $content = preg_replace('/# BEGIN RamboWoon SSL.*?# END RamboWoon SSL\s*/is', '', $content);
            file_put_contents('.htaccess', trim($content) . "\n");
        }

        // 2. Configure .env and .htaccess
        if (!empty($dbConfig)) {
            $this->updateEnv($dbConfig, $appConfig);
            $results[] = "Env configured";
        }

        $this->updateHtaccess($appConfig);
        $results[] = "Htaccess rules configured (SSL & Lock)";

        if (!empty($appConfig) && isset($appConfig['ssl']) && empty($_POST['skip_htaccess'])) {
            $this->updateHtaccess($appConfig);
            $results[] = "Htaccess configured (SSL: " . ($appConfig['ssl'] ? 'On' : 'Off') . ")";
        }

        $clearDb = isset($_POST['clear_db']) ? (int)$_POST['clear_db'] : 0;

        // 3. Import SQL
        if (file_exists($sqlFile)) {
            $sqlSize = filesize($sqlFile);
            $results[] = "Found SQL file: $sqlFile ($sqlSize bytes)";
            if ($sqlSize > 0) {
                try {
                    // Wait 2 seconds for DB permissions to propagate (DirectAdmin API delay)
                    sleep(2);

                    $host = $dbConfig['host'] ?? 'localhost';
                    $dbname = $dbConfig['name'] ?? '';
                    $user = $dbConfig['user'] ?? '';
                    $pass = $dbConfig['pass'] ?? '';

                    if ($clearDb) {
                        $results[] = "Clearing database tables first...";
                        $this->clearDatabase($dbConfig);
                    }

                    // Auto-replace Demo Domain with Production Domain in SQL file
                    if (!empty($appConfig['demo_domain']) && !empty($appConfig['prod_domain'])) {
                        $demoDom = trim($appConfig['demo_domain'], '/');
                        $prodDom = trim($appConfig['prod_domain'], '/');
                        if ($demoDom !== $prodDom) {
                            $sqlContent = file_get_contents($sqlFile);
                            $sqlContent = str_ireplace($demoDom, $prodDom, $sqlContent);
                            file_put_contents($sqlFile, $sqlContent);
                            $results[] = "Domain mapping applied to SQL: $demoDom -> $prodDom";
                        }
                    }

                    $hasExec = function_exists('exec');

                    // TRY METHOD 1: System MySQL Command (Better integrity)
                    if (function_exists('exec')) {
                        $outM = $resM = null;
                        @exec('mysql --version 2>&1', $outM, $resM);
                        if ($resM === 0) {
                            $passPart = !empty($pass) ? "-p'" . str_replace("'", "'\\''", $pass) . "'" : "";
                            $cmd = "mysql --host={$host} --user={$user} {$passPart} --default-character-set=utf8mb4 {$dbname} < \"{$sqlFile}\" 2>&1";
                            @exec($cmd, $importOut, $importRes);
                            if ($importRes === 0) {
                                $results[] = "Database imported via system command (mysql CLI)";
                                $isSqlSuccess = true;
                            } else {
                                $results[] = "System mysql import FAILED, falling back to PDO...";
                            }
                        }
                    }

                    if (!$isSqlSuccess) {
                        // METHOD 2: PDO Fallback
                        $pdo = $this->getPdoConnection($dbConfig);
                        $pdo->exec("SET NAMES 'utf8mb4' COLLATE 'utf8mb4_unicode_ci';");
                        $pdo->exec("SET CHARACTER SET utf8mb4;");
                        $sql = file_get_contents($sqlFile);


                        $pdo->exec("SET FOREIGN_KEY_CHECKS=0;");
                        $pdo->exec($sql);
                        $pdo->exec("SET FOREIGN_KEY_CHECKS=1;");
                        $results[] = "DB imported successfully via PDO ($sqlSize bytes)";
                        $isSqlSuccess = true;
                    }

                    // 4. Update Setting Table (Email config)
                    if ($isSqlSuccess && !empty($appConfig['email_host'])) {
                        $pdo = $this->getPdoConnection($dbConfig);
                        $this->updateSettingTable($pdo, $appConfig);
                    }
                    $results[] = "Database Import SUCCESS.";
                } catch (\Exception $e) {
                    $results[] = "DB Error: " . $e->getMessage();
                }
            } else {
                $results[] = "DB Import: $sqlFile is empty (0 bytes)";
            }
        } else {
            $results[] = "DB Import: $sqlFile NOT found on server";
            // Debug: List files in current dir
            $dirFiles = scandir('.');
            $results[] = "Current Dir Files: " . implode(', ', $dirFiles);
        }

        if ($isZipSuccess) @unlink($zipFile);
        if ($isSqlSuccess) @unlink($sqlFile);

        // 5. CONFIGURE HTACCESS (SSL & LOCK)
        $htaccess = '.htaccess';
        $skipLock = !empty($appConfig['skip_lock']);

        if (file_exists($htaccess)) {
            $content = file_get_contents($htaccess);

            // SSL Rules
            if (!empty($appConfig['ssl']) && strpos($content, 'HTTPS') === false) {
                $sslRules = "\nRewriteEngine On\nRewriteCond %{HTTPS} off\nRewriteRule ^(.*)$ https://%{HTTP_HOST}%{REQUEST_URI} [L,R=301]\n";
                $content = $sslRules . $content;
            }

            // Lock Rules (Skip if requested)
            if ($skipLock) {
                $results[] = "Htaccess: Skip lock (Demo mode).";
            } else if (strpos($content, 'lock.php') === false) {
                if (strpos($content, 'DirectoryIndex') !== false) {
                    $content = preg_replace('/DirectoryIndex\s+/i', 'DirectoryIndex lock.php ', $content);
                } else {
                    $content = "DirectoryIndex lock.php index.php\n" . $content;
                }
                $results[] = "Website LOCKED: lock.php is active.";
            } else {
                $results[] = "Website status: Already locked.";
            }
            file_put_contents($htaccess, $content);
        } else {
            if ($skipLock) {
                $results[] = "Htaccess: Not created (Skip lock).";
            } else {
                file_put_contents($htaccess, "DirectoryIndex lock.php index.php\n");
                $results[] = "Created .htaccess with lock.php active.";
            }
        }

        // 6. Final Cleanup: Remove unwanted hosting folders (e.g. cgi-bin)
        if (is_dir('cgi-bin')) {
            $this->removeRecursive('cgi-bin');
            $results[] = "Cleanup: Removed 'cgi-bin' folder.";
        }

        // 7. DONE
        echo json_encode(['status' => 'success', 'logs' => $results]);
    }

    private function getDbConfigFromEnv()
    {
        $db = ['host' => 'localhost', 'name' => '', 'user' => '', 'pass' => ''];
        if (!file_exists('.env')) return $db;

        $content = file_get_contents('.env');
        // Strip BOM if present
        $content = preg_replace('/^\xEF\xBB\xBF/', '', $content);
        $lines = explode("\n", str_replace(["\r\n", "\r"], "\n", $content));

        foreach ($lines as $line) {
            $line = trim($line);
            if (!$line || $line[0] === '#') continue;
            if (strpos($line, '=') === false) continue;

            $parts = explode('=', $line, 2);
            $key = strtoupper(trim($parts[0]));
            $val = trim($parts[1]);

            // Smart Comment Handling: Only strip if # is preceded by space or at start
            if (preg_match('/\s+#/', $val, $matches, PREG_OFFSET_CAPTURE)) {
                $val = trim(substr($val, 0, $matches[0][1]));
            }
            // Strip quotes and whitespace
            $val = trim($val, " \t\n\r\0\x0B\"'");

            if (in_array($key, ['DB_HOST', 'DATABASE_HOST'])) $db['host'] = $val;
            else if (in_array($key, ['DB_DATABASE', 'DB_NAME', 'DB_DB', 'DATABASE_NAME'])) $db['name'] = $val;
            else if (in_array($key, ['DB_USERNAME', 'DB_USER', 'DATABASE_USER', 'DATABASE_USERNAME'])) $db['user'] = $val;
            else if (in_array($key, ['DB_PASSWORD', 'DB_PASS', 'DATABASE_PASS', 'DATABASE_PASSWORD'])) $db['pass'] = $val;
        }
        return $db;
    }

    private function getPdoConnection($dbConfig)
    {
        $host = $dbConfig['host'] ?? 'localhost';
        $dbname = $dbConfig['name'] ?? '';
        $user = $dbConfig['user'] ?? '';
        $pass = $dbConfig['pass'] ?? '';

        try {
            $options = [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES 'utf8mb4' COLLATE 'utf8mb4_unicode_ci'"
            ];
            return new PDO("mysql:host={$host};dbname={$dbname};charset=utf8mb4", $user, $pass, $options);
        } catch (PDOException $e) {
            $msg = $e->getMessage();
            if (strpos($msg, '2002') !== false || strpos($msg, 'No such file or directory') !== false) {
                try {
                    return new PDO("mysql:host=127.0.0.1;dbname={$dbname};charset=utf8mb4", $user, $pass, $options);
                } catch (PDOException $e2) {
                    throw new Exception("DEBUG_BRIDGE: Localhost & 127.0.0.1 failed. Error 1: {$msg}. Error 2: " . $e2->getMessage());
                }
            }
            throw new Exception("DEBUG_BRIDGE: PDO Error on [$host]: " . $e->getMessage());
        }
    }

    private function updateEnv($dbConfig, $appConfig)
    {
        $env = '.env';
        if (!file_exists($env)) {
            // Create a fresh .env if missing
            file_put_contents($env, "ENVIRONMENT=production\n");
        }

        $lines = file($env, FILE_IGNORE_NEW_LINES);
        $newLines = [];
        $processedKeys = [];

        $fullUrl = $appConfig['app_url'] ?? '';
        $cleanUrl = "";
        $sitePath = "/";

        if (!empty($fullUrl)) {
            $parsed = parse_url($fullUrl);
            $cleanUrl = ($parsed['scheme'] ?? 'http') . '://' . ($parsed['host'] ?? '');
            $sitePath = '/' . trim($parsed['path'] ?? '', '/') . '/';
            if ($sitePath === '//') $sitePath = '/';
        }

        $keysToUpdate = ['DB_HOST', 'DB_DATABASE', 'DB_NAME', 'DB_DB', 'DB_USERNAME', 'DB_USER', 'DB_PASSWORD', 'DB_PASS', 'RANDOMKEY', 'SITE_PATH', 'APP_URL', 'ENVIRONMENT'];
        if (!empty($appConfig['is_production'])) {
            $keysToUpdate[] = 'TURNSTILE_SITEKEY';
            $keysToUpdate[] = 'TURNSTILE_SECRETKEY';
        }

        foreach ($lines as $line) {
            $trimmed = trim($line);
            if (empty($trimmed)) {
                $newLines[] = $line;
                continue;
            }

            $isComment = (strpos($trimmed, '#') === 0);
            $cleanLine = $isComment ? ltrim($trimmed, '# ') : $trimmed;

            $parts = explode('=', $cleanLine, 2);
            $key = strtoupper(trim($parts[0]));

            // If it's a comment and NOT a key we want to update, keep it as a comment
            if ($isComment && !in_array($key, $keysToUpdate)) {
                $newLines[] = $line;
                continue;
            }

            if (empty($key)) {
                $newLines[] = $line;
                continue;
            }

            $val = isset($parts[1]) ? trim($parts[1]) : '';

            if ($key === 'DB_HOST') {
                $val = "localhost";
            } else if (in_array($key, ['DB_DATABASE', 'DB_NAME', 'DB_DB'])) {
                if (!empty($dbConfig['name'])) $val = $dbConfig['name'];
            } else if (in_array($key, ['DB_USERNAME', 'DB_USER'])) {
                if (!empty($dbConfig['user'])) $val = $dbConfig['user'];
            } else if (in_array($key, ['DB_PASSWORD', 'DB_PASS'])) {
                if (!empty($dbConfig['pass'])) $val = $dbConfig['pass'];
            } else if ($key === 'RANDOMKEY' && !empty($appConfig['random_key'])) {
                $val = $appConfig['random_key'];
            } else if ($key === 'SITE_PATH') {
                $val = $sitePath;
            } else if ($key === 'APP_URL' && !empty($cleanUrl)) {
                $val = $cleanUrl . '${SITE_PATH}';
            } else if ($key === 'ENVIRONMENT') {
                $val = "production";
            } else if ($key === 'TURNSTILE_SITEKEY' && !empty($appConfig['is_production'])) {
                if (!empty($appConfig['turnstile_sitekey'])) $val = $appConfig['turnstile_sitekey'];
            } else if ($key === 'TURNSTILE_SECRETKEY' && !empty($appConfig['is_production'])) {
                if (!empty($appConfig['turnstile_secretkey'])) $val = $appConfig['turnstile_secretkey'];
            }

            $newLines[] = "{$key}={$val}";
            $processedKeys[] = $key;
        }

        // Add missing keys
        if (!in_array('DB_DATABASE', $processedKeys) && !empty($dbConfig['name'])) $newLines[] = "DB_DATABASE=" . $dbConfig['name'];
        if (!in_array('DB_USERNAME', $processedKeys) && !empty($dbConfig['user'])) $newLines[] = "DB_USERNAME=" . $dbConfig['user'];
        if (!in_array('DB_PASSWORD', $processedKeys) && !empty($dbConfig['pass'])) $newLines[] = "DB_PASSWORD=" . $dbConfig['pass'];
        if (!in_array('DB_HOST', $processedKeys)) $newLines[] = "DB_HOST=localhost";
        if (!in_array('ENVIRONMENT', $processedKeys)) $newLines[] = "ENVIRONMENT=production";

        if (!empty($appConfig['is_production']) && !empty($appConfig['turnstile_sitekey'])) {
            if (!in_array('TURNSTILE_SITEKEY', $processedKeys)) $newLines[] = "TURNSTILE_SITEKEY=" . $appConfig['turnstile_sitekey'];
            if (!in_array('TURNSTILE_SECRETKEY', $processedKeys)) $newLines[] = "TURNSTILE_SECRETKEY=" . $appConfig['turnstile_secretkey'];
        }

        file_put_contents($env, implode(PHP_EOL, $newLines));
    }

    private function updateSettingTable($pdo, $appConfig)
    {
        try {
            $stmt = $pdo->query("SELECT id, options FROM table_setting LIMIT 1");
            $row = $stmt->fetch(PDO::FETCH_ASSOC);
            if ($row) {
                $options = json_decode($row['options'] ?? '{}', true);
                if (!is_array($options)) $options = [];

                $options['ip_host'] = !empty($appConfig['smtp_host']) ? $appConfig['smtp_host'] : 'localhost';
                $options['email_host'] = !empty($appConfig['email_user']) ? $appConfig['email_user'] : ($options['email_host'] ?? '');
                $options['password_host'] = !empty($appConfig['email_pass']) ? $appConfig['email_pass'] : ($options['password_host'] ?? '');

                $newOptions = json_encode($options, JSON_UNESCAPED_UNICODE);
                $update = $pdo->prepare("UPDATE table_setting SET options = ? WHERE id = ?");
                $update->execute([$newOptions, $row['id']]);
            }
        } catch (Exception $e) {
            // Silently fail or log for debug
        }
    }

    private function updateHtaccess($appConfig)
    {
        $htaccess = '.htaccess';
        if (!file_exists($htaccess)) {
            file_put_contents($htaccess, "RewriteEngine On\nOptions -Indexes\n");
        }

        $content = file_get_contents($htaccess);
        $sslEnabled = !empty($appConfig['ssl']);
        $skipLock = !empty($appConfig['skip_lock']);

        // 1. Handle SSL Redirection
        if ($sslEnabled) {
            // Fix any wrong direction: RewriteCond %{HTTPS} on → off
            $content = str_ireplace('RewriteCond %{HTTPS} on', 'RewriteCond %{HTTPS} off', $content);

            // Fix any http:// redirect targets → https://
            $content = preg_replace('/(RewriteRule\s+.*?\s+)http:\/\//i', '$1https://', $content);
        } else {
            // Remove existing SSL block if present
            $content = preg_replace('/# BEGIN RamboWoon SSL.*?# END RamboWoon SSL\s*/is', '', $content);
        }

        // 2. Handle Lock mechanism (DirectoryIndex)
        $content = str_replace('lock.php ', '', $content);
        $content = str_replace(' lock.php', '', $content);

        if (!$skipLock) {
            if (strpos($content, 'DirectoryIndex') !== false) {
                $content = preg_replace('/DirectoryIndex\s+/i', 'DirectoryIndex lock.php ', $content);
            } else {
                $content = "DirectoryIndex lock.php index.php\n" . $content;
            }
        } else {
            if (strpos($content, 'DirectoryIndex') !== false) {
                $content = str_replace('lock.php', '', $content);
            }
        }

        file_put_contents($htaccess, trim($content) . "\n");
    }

    private function removeRecursive($dir)
    {
        if (!is_dir($dir)) return @unlink($dir);
        foreach (scandir($dir) as $item) {
            if ($item == '.' || $item == '..') continue;
            if (!$this->removeRecursive($dir . DIRECTORY_SEPARATOR . $item)) return false;
        }
        return rmdir($dir);
    }

    public function deployDb()
    {
        $results = [];
        $results[] = "--- Starting DB-Only Import [Bridge {$this->version}] ---";
        $sqlFile = 'dist.sql';

        $searchPaths = ['.', '..', '../..'];
        foreach ($searchPaths as $path) {
            if (!file_exists($sqlFile) && file_exists($path . '/dist.sql')) {
                $sqlFile = $path . '/dist.sql';
                $results[] = "SQL found in: $sqlFile";
            }
        }

        if (!file_exists($sqlFile)) {
            die(json_encode(['status' => 'error', 'message' => 'Lỗi: Không tìm thấy tệp dist.sql trên server.', 'logs' => $results]));
        }

        $dbConfig = $this->getDbConfigFromEnv();
        if (empty($dbConfig['name'])) {
            die(json_encode(['status' => 'error', 'message' => 'Lỗi: Không tìm thấy thông tin cấu hình database trong file .env trên server.', 'logs' => $results]));
        }

        $results[] = "Loaded DB config from server .env (Host=" . ($dbConfig['host'] ?? 'localhost') . ", DB=" . ($dbConfig['name'] ?? 'none') . ")";
        $isSqlSuccess = false;

        $clearDb = isset($_POST['clear_db']) ? (int)$_POST['clear_db'] : 0;

        try {
            $host = $dbConfig['host'] ?? 'localhost';
            $dbname = $dbConfig['name'] ?? '';
            $user = $dbConfig['user'] ?? '';
            $pass = $dbConfig['pass'] ?? '';

            if ($clearDb) {
                $results[] = "Clearing database tables first...";
                $this->clearDatabase($dbConfig);
            }

            if (function_exists('exec')) {
                $outM = $resM = null;
                @exec('mysql --version 2>&1', $outM, $resM);
                if ($resM === 0) {
                    $passPart = !empty($pass) ? "-p'" . str_replace("'", "'\\''", $pass) . "'" : "";
                    $cmd = "mysql --host={$host} --user={$user} {$passPart} --default-character-set=utf8mb4 {$dbname} < \"{$sqlFile}\" 2>&1";
                    @exec($cmd, $importOut, $importRes);
                    if ($importRes === 0) {
                        $results[] = "Database imported via system mysql command";
                        $isSqlSuccess = true;
                    } else {
                        $results[] = "System mysql import FAILED, falling back to PDO...";
                    }
                }
            }

            if (!$isSqlSuccess) {
                $pdo = $this->getPdoConnection($dbConfig);
                $pdo->exec("SET NAMES 'utf8mb4' COLLATE 'utf8mb4_unicode_ci';");
                $pdo->exec("SET CHARACTER SET utf8mb4;");
                $sql = file_get_contents($sqlFile);
                $pdo->exec("SET FOREIGN_KEY_CHECKS=0;");
                $pdo->exec($sql);
                $pdo->exec("SET FOREIGN_KEY_CHECKS=1;");
                $results[] = "Database imported successfully via PDO";
                $isSqlSuccess = true;
            }
        } catch (\Exception $e) {
            die(json_encode(['status' => 'error', 'message' => 'Lỗi import DB: ' . $e->getMessage(), 'logs' => $results]));
        }

        if ($isSqlSuccess) {
            @unlink($sqlFile);
            echo json_encode(['status' => 'success', 'logs' => $results]);
        } else {
            echo json_encode(['status' => 'error', 'message' => 'Không thể import database.', 'logs' => $results]);
        }
        exit;
    }

    public function checkDb()
    {
        header('Content-Type: application/json');

        $dbRaw = $_POST['db_config'] ?? null;
        if (!$dbRaw) {
            $rawInput = file_get_contents('php://input');
            if (!empty($rawInput)) {
                parse_str($rawInput, $inputData);
                $dbRaw = $inputData['db_config'] ?? null;
            }
        }

        $dbConfig = json_decode($dbRaw ?? '', true);
        if (empty($dbConfig) || empty($dbConfig['name'])) {
            $dbConfig = $this->getDbConfigFromEnv();
        }

        if (empty($dbConfig) || empty($dbConfig['name'])) {
            die(json_encode(['status' => 'error', 'message' => 'No database configuration found']));
        }

        try {
            $pdo = $this->getPdoConnection($dbConfig);
            $stmt = $pdo->query("SHOW TABLES");
            $tables = $stmt->fetchAll(PDO::FETCH_COLUMN);
            $hasData = count($tables) > 0;
            echo json_encode(['status' => 'success', 'has_data' => $hasData]);
        } catch (\Exception $e) {
            echo json_encode(['status' => 'error', 'message' => $e->getMessage()]);
        }
        exit;
    }

    private function clearDatabase($dbConfig)
    {
        try {
            $pdo = $this->getPdoConnection($dbConfig);
            $pdo->exec("SET FOREIGN_KEY_CHECKS=0;");

            // Get all tables
            $stmt = $pdo->query("SHOW TABLES");
            $tables = $stmt->fetchAll(PDO::FETCH_COLUMN);

            foreach ($tables as $table) {
                $pdo->exec("DROP TABLE IF EXISTS `{$table}`");
            }

            $pdo->exec("SET FOREIGN_KEY_CHECKS=1;");
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }

    private function clearFolderContent($dir)
    {
        if (!is_dir($dir)) return;
        foreach (scandir($dir) as $item) {
            if ($item == '.' || $item == '..') continue;
            $path = $dir . DIRECTORY_SEPARATOR . $item;
            if (is_dir($path)) {
                $this->clearFolderContent($path);
                @rmdir($path);
            } else {
                if ($item !== '.gitignore') {
                    @unlink($path);
                }
            }
        }
    }

    public function cleanup()
    {
        echo json_encode(['status' => 'success', 'message' => 'Bridge self-destructed successfully.']);
        $zipFile = 'dist.zip';
        $sqlFile = 'dist.sql';
        @unlink($zipFile);
        @unlink($sqlFile);
        @unlink(__FILE__);
        exit;
    }
}

// Instantiate and Run the Bridge
$bridge = new RamboWoonBridge();
$bridge->run();
