import 'dart:convert';
import 'dart:io';

import 'package:shared_preferences/shared_preferences.dart';

class ApiException implements Exception {
  ApiException(this.status, this.message);
  final int status;
  final String message;

  @override
  String toString() => message;
}

class Api {
  Api._() {
    _client.connectionTimeout = const Duration(seconds: 10);
  }

  static final Api instance = Api._();

  static const _override = String.fromEnvironment('API_BASE_URL');
  // The Android emulator reaches the host machine on 10.0.2.2; the iOS simulator on localhost.
  static final String baseUrl = _override.isNotEmpty
      ? _override
      : (Platform.isAndroid ? 'http://10.0.2.2:5133/api' : 'http://localhost:5133/api');

  final HttpClient _client = HttpClient();
  String? token;

  Future<void> loadToken() async {
    token = (await SharedPreferences.getInstance()).getString('token');
  }

  Future<void> saveToken(String? value) async {
    token = value;
    final prefs = await SharedPreferences.getInstance();
    if (value == null) {
      await prefs.remove('token');
    } else {
      await prefs.setString('token', value);
    }
  }

  Future<dynamic> get(String path) => _send('GET', path);
  Future<dynamic> post(String path, [Object? body]) => _send('POST', path, body);
  Future<dynamic> put(String path, Object? body) => _send('PUT', path, body);
  Future<dynamic> delete(String path) => _send('DELETE', path);

  Future<String> upload(File file) async {
    const boundary = '----goschool';
    final name = file.uri.pathSegments.last;
    final request = await _client.postUrl(Uri.parse('$baseUrl/uploads'));
    _authorize(request);
    request.headers.set(HttpHeaders.contentTypeHeader, 'multipart/form-data; boundary=$boundary');
    request.add(utf8.encode('--$boundary\r\nContent-Disposition: form-data; name="file"; filename="$name"\r\n'
        'Content-Type: application/octet-stream\r\n\r\n'));
    request.add(await file.readAsBytes());
    request.add(utf8.encode('\r\n--$boundary--\r\n'));
    final json = await _read(await request.close());
    return json['url'] as String;
  }

  Future<dynamic> _send(String method, String path, [Object? body]) async {
    final request = await _client.openUrl(method, Uri.parse('$baseUrl$path'));
    _authorize(request);
    if (body != null) {
      request.headers.contentType = ContentType.json;
      request.write(jsonEncode(body));
    }
    return _read(await request.close());
  }

  void _authorize(HttpClientRequest request) {
    if (token != null) request.headers.set(HttpHeaders.authorizationHeader, 'Bearer $token');
  }

  Future<dynamic> _read(HttpClientResponse response) async {
    final text = await utf8.decodeStream(response);
    final json = text.isEmpty ? <String, dynamic>{} : jsonDecode(text);
    if (response.statusCode >= 200 && response.statusCode < 300) return json;
    if (response.statusCode == 401) throw ApiException(401, 'Session expired. Please log in again.');
    throw ApiException(response.statusCode, _message(json, text));
  }

  static String _message(dynamic json, String text) {
    if (json is Map) {
      if (json['message'] != null) return json['message'].toString();
      final errors = json['errors'];
      if (errors is Map && errors.isNotEmpty) return errors.values.expand((v) => v is List ? v : [v]).join('\n');
      if (json['title'] != null) return json['title'].toString();
    }
    return text.isEmpty ? 'Request failed' : text;
  }
}
