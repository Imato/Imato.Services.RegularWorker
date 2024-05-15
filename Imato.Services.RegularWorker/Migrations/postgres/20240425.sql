alter table workers
	add error text;


create or replace function set_worker(
	_id int,
	_name text,
	_appName text,
	_host text,
	_active boolean,
	_settings text,
	_error text = '')
returns setof workers
as 
$$
declare 
	_hosts int = 0;
	_activeHosts int = 0;
begin	
 
	select count(1), iif(active = true, 1, 0)
		into _hosts, _activeHosts
		from workers 
		where name = _name 
			and date >= now() - (60 * interval'1 second')
			and host != _host;

	_hosts := _hosts + 1;
	_activeHosts := coalesce(_activeHosts, 0);

	update workers
		set active = _active, 
			date = current_timestamp,
			hosts = _hosts,
			error = _error
		where (_id > 0 and id = _id) or (host = _host and name = _name and appName = _appName)
		returning id
		into _id;
	
	if (_id is null or _id = 0)
		then 
		insert into workers 
			(name, host, appName, date, settings, active, hosts, error)
		values 
			(_name, _host, _appName, current_timestamp, _settings, _active, _hosts, _error)
		returning id
		into _id;		
	end if;
		
 return query
 (select *, _activeHosts as activeHosts
		from workers 
		where id = _id);
	
end
$$ language plpgsql;