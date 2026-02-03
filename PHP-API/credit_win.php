<?php
/* =======================================================
   CARROM 1v1 - CREDIT WINNINGS API
   Endpoint: https://tasktrophy.in/games/carrom/api/credit_win.php
   ======================================================= */
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST');

require_once($_SERVER['DOCUMENT_ROOT'] . '/wp-load.php');
global $wpdb;

// SECURITY: Verify secret key
if (!isset($_POST['secret_key']) || $_POST['secret_key'] !== "TaskTrophy_Secure_2026") {
    die(json_encode(["status" => "error", "msg" => "Unauthorized"]));
}

$user_id = get_current_user_id();
if (!$user_id) {
    die(json_encode(["status" => "error", "msg" => "Login Required"]));
}

// CREDIT Rs 18
$prize = 18;
$table = $wpdb->prefix . 'users';
$wpdb->query($wpdb->prepare("UPDATE $table SET wallet_balance = wallet_balance + %f WHERE ID = %d", $prize, $user_id));

// Get new balance
$new_balance = $wpdb->get_var($wpdb->prepare("SELECT wallet_balance FROM $table WHERE ID = %d", $user_id));

echo json_encode(["status" => "success", "prize" => $prize, "new_balance" => $new_balance]);
?>
