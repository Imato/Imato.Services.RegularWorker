select table_name 
	from information_schema.tables 
	where table_name like 'config%' 
	limit 1;