create or replace function set_worker(
	_id int,
	_name text,
	_appName text,
	_host text,
	_active boolean,
	_settings text)
returns setof workers
as 
$$
declare 
	_hosts int = 0;
begin	
 
	select count(1) 
		into _hosts
		from workers 
		where name = _name 
			and date >= now() - (60 * interval'1 second')
			and host != _host
			and active = true;

	_hosts := _hosts + 1;

	update workers
		set active = _active, 
			date = current_timestamp,
			hosts = _hosts
		where (_id > 0 and id = _id) or (host = _host and name = _name and appName = _appName)
		returning id
		into _id;
	
	if (_id is null or _id = 0)
		then 
		insert into workers 
			(name, host, appName, date, settings, active, hosts)
		values 
			(_name, _host, _appName, current_timestamp, _settings, _active, _hosts)
		returning id
		into _id;		
	end if;
		
 return query
 (select * 
		from workers 
		where id = _id);
	
end
$$ language plpgsql;


