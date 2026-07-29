<?php
/**
 * SKIPPY AI Request endpoint
 */

$AUTH_KEY = 'skippy-roast-key-change-me'; # skippy auth key
$DEEPSEEK_KEY = 'sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'; # api key
$MODEL = 'deepseek-v4-pro'; # model
$API_BASE = 'https://api.deepseek.com/v1'; # 别改，除非 ds 换接口了

// prompt

$SYSTEM = <<<'EOT'
你是一个毒舌的 SCP 基金会文章审稿人。你的任务是对用户提交的 SCP 文档进行犀利、幽默、带讽刺意味的吐槽和点评。

规则：
1. 用中文回复，语气要毒舌但不失幽默，像一个挑剔的基金会高级研究员在审阅新人的草稿
2. 指出文章中不合理、不合规、不专业的地方
3. 可以吐槽格式问题、逻辑漏洞、设定矛盾、语言表达等
4. 回复控制在 200 字以内
5. 如果文章内容格式混乱或者有明显问题，可以更狠地吐槽
6. 始终以 SCP 基金会的口吻进行吐槽，可以用一些基金会术语
7. 如果收到的不是 SCP 相关内容，吐槽用户浪费你的时间
EOT;

// main logic

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['comment' => '仅支持 POST 请求。'], JSON_UNESCAPED_UNICODE);
    exit;
}

// 防小人不妨君子这一块
$clientKey = $_POST['api_key'] ?? '';

if (!hash_equals($AUTH_KEY, $clientKey)) {
    http_response_code(403);
    echo json_encode(['comment' => 'API Key 无效。'], JSON_UNESCAPED_UNICODE);
    exit;
}

$content = trim($_POST['api_content'] ?? '');

// 别他妈发纯空格
$content = trim($content);

if ($content === '' || $content === null) {
    http_response_code(400);
    echo json_encode(['comment' => '请提供文章内容 (api_content)。'], JSON_UNESCAPED_UNICODE);
    exit;
}

$oldLen = mb_strlen($content);
if ($oldLen > 10000) {
    $content = mb_substr($content, 0, 10000) . "\n\n[内容过长，已截断至 10000 字符]";
}

$payload = [
    'model'       => $MODEL,
    'messages'    => [
        ['role' => 'system', 'content' => $SYSTEM],
        ['role' => 'user',   'content' => $content],
    ],
    'temperature'  => 0.9,
    'max_tokens'   => 500,
    'stream'       => false,
];

$ch = curl_init($API_BASE . '/chat/completions');

curl_setopt_array($ch, [
    CURLOPT_POST           => true,
    CURLOPT_POSTFIELDS     => json_encode($payload, JSON_UNESCAPED_UNICODE),
    CURLOPT_HTTPHEADER     => [
        'Content-Type: application/json',
        'Authorization: Bearer ' . $DEEPSEEK_KEY,
    ],
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_TIMEOUT        => 30,
    CURLOPT_CONNECTTIMEOUT => 10,
    // CURLOPT_VERBOSE => true,
]);

$raw   = curl_exec($ch);
$http  = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$err   = curl_error($ch);


// 网络错误
if ($err) {
    http_response_code(502);
    echo json_encode(['comment' => 'DeepSeek API 连接失败：' . $err], JSON_UNESCAPED_UNICODE);
    exit;
}

if ($http !== 200) {
    $errBody = json_decode($raw, true);
    $msg = '';
    if (is_array($errBody)) {
        $msg = $errBody['error']['message'] ?? '';
    }
    if ($msg === '') {
        $msg = "HTTP $http";
    }
    $msg = trim($msg);
    http_response_code(502);
    echo json_encode(['comment' => "DeepSeek API 返回错误：$msg"], JSON_UNESCAPED_UNICODE);
    exit;
}

$data    = json_decode($raw, true);

$comment = '';
$comment = $data['choices'][0]['message']['content'];

// FUCK U REGEX
// 但是很好用
$comment = preg_replace('/^(吐槽[：:]?\s*|点评[：:]?\s*|审稿意见[：:]?\s*)/u', '', $comment);

$comment = trim($comment);
if ($comment === '' || $comment === null) {
    $comment = '写的什么逼玩意，show show way';
}
echo json_encode(['comment' => $comment], JSON_UNESCAPED_UNICODE);