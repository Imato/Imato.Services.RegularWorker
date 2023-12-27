select top 1 schema_name(t.schema_id) + '.' + t.name 
	from sys.tables t 
	where name like 'config%'