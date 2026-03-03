using System.Data.Common;
using DigitaleDelta.Contracts.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitaleDelta.DbRowMaterializer.Tests;

public class DbRowMaterializerTests
{
    // Helper: create a fake reader for unit tests
    private class TestDataReader : DbDataReader
    {
        private readonly List<object[]> _rows;
        private int _rowIndex = -1;
        private readonly string[] _columns;
        private readonly Type[] _columnTypes;

        public TestDataReader(params (string Name, Type Type)[] schema)
        {
            _columns = schema.Select(x => x.Name).ToArray();
            _columnTypes = schema.Select(x => x.Type).ToArray();
            _rows = [];
        }

        public void AddRow(params object[] values) => _rows.Add(values);

        public override bool Read() => ++_rowIndex < _rows.Count;

        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());
        public override int FieldCount => _columns.Length;
        public override string GetName(int ordinal) => _columns[ordinal];
        public override object GetValue(int ordinal) => _rows[_rowIndex][ordinal];
        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;
        public override double GetDouble(int ordinal) => Convert.ToDouble(_rows[_rowIndex][ordinal]);

        public override Type GetFieldType(int ordinal) => _columnTypes[ordinal];
        public override float GetFloat(int ordinal) => Convert.ToSingle(_rows[_rowIndex][ordinal]);

        public override Guid GetGuid(int ordinal) => Guid.Parse(_rows[_rowIndex][ordinal].ToString());

        public override short GetInt16(int ordinal) => Convert.ToInt16(_rows[_rowIndex][ordinal]);

        public override int GetOrdinal(string name) => Array.IndexOf(_columns, name);
        public override T GetFieldValue<T>(int ordinal) => (T)GetValue(ordinal);

        // Other abstract members - minimal implementation
        public override bool HasRows => _rows.Any();
        public override int Depth => 0;
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override object this[int ordinal] => GetValue(ordinal);
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override void Close() { }
        public override bool GetBoolean(int ordinal) => Convert.ToBoolean(_rows[_rowIndex][ordinal]);

        public override byte GetByte(int ordinal) => Convert.ToByte(_rows[_rowIndex][ordinal]);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal) => Convert.ToChar(_rows[_rowIndex][ordinal]);

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(_rows[_rowIndex][ordinal]);

        public override bool NextResult() => false;
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);
        // Required but unused
        public override IEnumerator<object[]> GetEnumerator() => throw new NotImplementedException();
        public override int GetValues(object[] values)
        {
            var arr = _rows[_rowIndex];
            arr.CopyTo(values, 0);
            return arr.Length;
        }
    }

    private static ODataToSqlMap Map(string odataProp, string query, string edmType) =>
        new ODataToSqlMap { ODataPropertyName = odataProp, Query = query, EdmType = edmType, ColumnName = query };

    [Fact]
    public async Task MaterializeToListAsync_BasicMapping_Works()
    {
        // Arrange
        var reader = new TestDataReader(("name", typeof(string)), ("val", typeof(int)));
        reader.AddRow("abc", 123);
        reader.AddRow("def", 456);
        var logger = Mock.Of<ILogger>();

        var maps = new[]
        {
            Map("Name", "name", "Edm.String"),
            Map("Value", "val", "Edm.Int32")
        }.ToDictionary(a => a.ODataPropertyName);

        // Act
        var result = await DbRowMaterializer.MaterializeToListAsync(reader, maps, suppressNulls: false, 2, logger, null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("abc", result[0]["Name"]);
        Assert.Equal(123, result[0]["Value"]);
        Assert.Equal("def", result[1]["Name"]);
        Assert.Equal(456, result[1]["Value"]);
    }

    [Fact]
    public async Task MaterializeToListAsync_SuppressNulls_RemovesNulls()
    {
        var reader = new TestDataReader(("col1", typeof(string)), ("col2", typeof(int?)));
        reader.AddRow(null, 42);
        reader.AddRow("foo", DBNull.Value);
        var logger = Mock.Of<ILogger>();

        var maps = new[]
        {
            Map("S", "col1", "Edm.String"),
            Map("I", "col2", "Edm.Int32"),
        }.ToDictionary(a => a.ODataPropertyName);

        var result = await DbRowMaterializer.MaterializeToListAsync(reader, maps, suppressNulls: true, 2, logger, null);

        Assert.Equal(2, result.Count);
        Assert.False(result[0].ContainsKey("S"));
        Assert.True(result[0].ContainsKey("I"));
        Assert.True(result[1].ContainsKey("S"));
        Assert.False(result[1].ContainsKey("I"));
    }

    [Fact]
    public async Task MaterializeToListAsync_DifferentEdmTypes_AreMappedCorrectly()
    {
        var reader = new TestDataReader(
            ("b", typeof(bool)),
            ("d", typeof(decimal)),
            ("dt", typeof(DateTimeOffset)));
        var now = DateTimeOffset.UtcNow;
        reader.AddRow(true, 7.5m, now);
        var logger = Mock.Of<ILogger>();

        var maps = new[]
        {
            Map("B", "b", "Edm.Boolean"),
            Map("D", "d", "Edm.Decimal"),
            Map("DT", "dt", "Edm.DateTimeOffset"),
        }.ToDictionary(a => a.ODataPropertyName);

        var result = await DbRowMaterializer.MaterializeToListAsync(reader, maps, false, 2, logger, null);

        Assert.True(result[0]["B"] is bool);
        Assert.True(result[0]["D"] is decimal);
        Assert.True(result[0]["DT"] is DateTimeOffset);
    }
}
