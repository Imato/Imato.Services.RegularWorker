create or replace function set_worker(
	_id int,
	_name text,
	_appName text,
	_host text,
	_active boolean,
	_settings text,
	_date timestamp)
returns setof workers
as 
$$
begin	
	update workers
		set active = _active, date = _date
		where (_id > 0 and id = _id) or (host = _host and name = _name and appName = _appName)
		returning id
		into _id;
	
	if (_id is null or _id = 0)
		then 
		insert into workers 
			(name, host, appName, date, settings, active)
		values 
			(_name, _host, _appName, _date, _settings, _active)
		returning id
		into _id;		
	end if;
		
 return query
 (select * 
		from workers 
		where id = _id);
end
$$ language plpgsql;