<?php
/* =======================================================
   CARROM 1v1 - DEDUCT ENTRY FEE API
   Endpoint: https://tasktrophy.in/games/carrom/api/deduct_fee.php
   ======================================================= */
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, GET');

require_once($_SERVER['DOCUMENT_ROOT'] . '/wp-load.php');
global $wpdb;

$user_id = get_current_user_id();
if (!$user_id) {
    die(json_encode(["status" => "error", "msg" => "Login Required"]));
}

// CHECK & DEDUCT Rs 10
$fee = 10;
$table = $wpdb->prefix . 'users';
$balance = $wpdb->get_var($wpdb->prepare("SELECT wallet_balance FROM $table WHERE ID = %d", $user_id));

if ($balance < $fee) {
    die(json_encode(["status" => "error", "msg" => "Low Balance"]));
}

$wpdb->query($wpdb->prepare("UPDATE $table SET wallet_balance = wallet_balance - %d WHERE ID = %d", $fee, $user_id));
echo json_encode(["status" => "success", "new_balance" => $balance - $fee, "user_id" => $user_id]);
?>
