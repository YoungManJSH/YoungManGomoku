#if USE_AWS_MYSQL && USE_URL_MSSQL
// AWS랑 URL 로컬서버를 동시에 define하는게 말이 되냐
#error "USE_AWS_MYSQL and USE_URL_MSSQL cannot be defined at the same time."
#endif

#if !USE_AWS_MYSQL && !USE_URL_MSSQL && !USE_URL_MSSQL_IIS
// AWS도 URL도 아니면 도대체 뭔데
#error "No DB provider defined. Define one of USE_AWS_MYSQL or USE_URL_MSSQL or !USE_URL_MSSQL_IIS."
#endif