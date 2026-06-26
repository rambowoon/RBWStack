<?php
/**
 * phpMyAdmin configuration for RBWStack (local development)
 */

declare(strict_types=1);

/**
 * Blowfish secret - PHẢI đúng 32 bytes
 */
$cfg['blowfish_secret'] = 'RBWStack@LocalDev#2024!SecureK32';

/**
 * Servers configuration
 */
$i = 0;
$i++;

$cfg['Servers'][$i]['auth_type']     = 'cookie';
$cfg['Servers'][$i]['host']          = '127.0.0.1';
$cfg['Servers'][$i]['port']          = '3306';
$cfg['Servers'][$i]['compress']      = false;
$cfg['Servers'][$i]['AllowNoPassword'] = true;
$cfg['Servers'][$i]['AllowRoot']     = true;

/* phpMyAdmin configuration storage */
$cfg['Servers'][$i]['pmadb']               = 'phpmyadmin';
$cfg['Servers'][$i]['bookmarktable']       = 'pma__bookmark';
$cfg['Servers'][$i]['relation']            = 'pma__relation';
$cfg['Servers'][$i]['table_info']          = 'pma__table_info';
$cfg['Servers'][$i]['table_coords']        = 'pma__table_coords';
$cfg['Servers'][$i]['pdf_pages']           = 'pma__pdf_pages';
$cfg['Servers'][$i]['column_info']         = 'pma__column_info';
$cfg['Servers'][$i]['history']             = 'pma__history';
$cfg['Servers'][$i]['table_uiprefs']       = 'pma__table_uiprefs';
$cfg['Servers'][$i]['tracking']            = 'pma__tracking';
$cfg['Servers'][$i]['userconfig']          = 'pma__userconfig';
$cfg['Servers'][$i]['recent']              = 'pma__recent';
$cfg['Servers'][$i]['favorite']            = 'pma__favorite';
$cfg['Servers'][$i]['users']               = 'pma__users';
$cfg['Servers'][$i]['usergroups']          = 'pma__usergroups';
$cfg['Servers'][$i]['navigationhiding']    = 'pma__navigationhiding';
$cfg['Servers'][$i]['savedsearches']       = 'pma__savedsearches';
$cfg['Servers'][$i]['central_columns']     = 'pma__central_columns';
$cfg['Servers'][$i]['designer_settings']   = 'pma__designer_settings';
$cfg['Servers'][$i]['export_templates']    = 'pma__export_templates';

/**
 * Tắt bắt buộc HTTPS
 */
$cfg['ForceSSL'] = false;

/**
 * Session cookie - cho phép HTTP
 */
ini_set('session.cookie_secure', '0');
ini_set('session.cookie_httponly', '1');
ini_set('session.cookie_samesite', 'Lax');
ini_set('session.cookie_lifetime', '2592000'); // Lưu session cookie 30 ngày để tránh mất đăng nhập khi khởi động lại máy/đóng trình duyệt
ini_set('session.gc_maxlifetime', '2592000'); // 30 ngày = khớp với LoginCookieStore/Validity

/**
 * Session save path
 */
$sessionPath = __DIR__ . '/../tmp/sessions';
if (!is_dir($sessionPath)) {
    @mkdir($sessionPath, 0777, true);
}
ini_set('session.save_path', $sessionPath);

/**
 * Upload/Save directories
 */
$cfg['UploadDir'] = '';
$cfg['SaveDir']   = '';

/**
 * Misc
 */
$cfg['CheckConfigurationPermissions'] = false;
$cfg['LoginCookieValidity']           = 2592000;
$cfg['LoginCookieStore']              = 2592000; // Lưu cookie trên trình duyệt không bị xóa khi tắt
$cfg['DefaultLang']                   = 'en';
$cfg['SendErrorReports']              = 'never';
