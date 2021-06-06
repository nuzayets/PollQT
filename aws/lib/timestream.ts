import * as cdk from 'monocdk';
import { RemovalPolicy } from 'monocdk';
import * as timestream from 'monocdk/aws-timestream';

import { DBNAME, TBLNAME } from './constants';

export class TimestreamStack extends cdk.Stack {
  readonly db : timestream.CfnDatabase;
  readonly table : timestream.CfnTable;
  
  constructor(scope: cdk.Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);
    this.db = new timestream.CfnDatabase(this, `Timestream-${props?.env?.region}`, {
      databaseName: DBNAME,
    });
    this.db.applyRemovalPolicy(RemovalPolicy.RETAIN);
  
    this.table = new timestream.CfnTable(this, `Timestream-${props?.env?.region}-Qt`, {
      tableName: TBLNAME,
      databaseName: DBNAME,
      retentionProperties: {
        MemoryStoreRetentionPeriodInHours: (24 * 30).toString(10),
        MagneticStoreRetentionPeriodInDays: (365 * 20).toString(10),
      }
    });

    this.table.node.addDependency(this.db);
    this.table.applyRemovalPolicy(RemovalPolicy.RETAIN);
  }
}
