-- Migration 001: user_sessions 테이블 생성
-- session_settings JSONB 구조:
-- {
--   "currentMode":         1,          -- AIModelType enum 값
--   "temperature":         0.5,
--   "maxTokens":           null,
--   "metadata":            {},         -- Dictionary<string, string>
--   "customSystemPrompts": {}          -- Dictionary<string, string> (모델명 → 프롬프트)
-- }

CREATE TABLE IF NOT EXISTS user_sessions (
    chat_id          BIGINT      PRIMARY KEY,
    session_settings JSONB       NOT NULL DEFAULT '{"currentMode":1,"temperature":0.5,"maxTokens":null,"metadata":{},"customSystemPrompts":{}}',
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_active_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 오래된 세션 정리 / 최근 활성 사용자 조회용
CREATE INDEX IF NOT EXISTS idx_user_sessions_last_active_at
    ON user_sessions (last_active_at DESC);

-- JSONB 내부 필드 검색용 GIN 인덱스 (currentMode, customSystemPrompts 키 조회 등)
CREATE INDEX IF NOT EXISTS idx_user_sessions_settings_gin
    ON user_sessions USING GIN (session_settings);
