export const DBNAME = process.env.TIMESTREAM_DBNAME ?? '';
export const TBLNAME = 'PollQT';

if (!DBNAME) throw 'Database name not defined in env TIMESTREAM_DBNAME.';