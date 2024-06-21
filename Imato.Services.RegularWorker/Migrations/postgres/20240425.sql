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
returns table 
	(id int,
	name varchar(255), 
	host varchar(255), 
	appName varchar(512), 
	date timestamp with time zone, 
	settings text, 
	active bool, 
	executed timestamp with time zone, 
	hosts int, 
	error text,
	activeHosts int)
as 
$$
declare 
	_hosts int = 0;
	_activeHosts int = 0;
begin	
 
	select count(1), sum(iif(w.active, 1, 0))
		into _hosts, _activeHosts
		from workers w
		where w.name = _name 
			and w.date >= now() - (60 * interval'1 second')
			and w.host != _host;

	_hosts := _hosts + 1;
	_activeHosts := coalesce(_activeHosts, 0);

	update workers w
		set active = _active, 
			date = current_timestamp,
			hosts = _hosts,
			error = _error
		where (_id > 0 and w.id = _id) or (w.host = _host and w.name = _name and w.appName = _appName)
		returning w.id
		into _id;
	
	if (_id is null or _id = 0)
		then 
		insert into workers
			(name, host, appName, date, settings, active, hosts, error)
		values 
			(_name, _host, _appName, current_timestamp, _settings, _active, _hosts, _error);
		-- returning id
		-- into _id;		
	end if;
		
 return query
 (select w.id, w.name, w.host, w.appName, w.date,
 w.settings, w.active, w.executed, w.hosts, w.error, _activeHosts as activeHosts
		from workers w
		where (_id > 0 and w.id = _id) or (w.host = _host and w.name = _name and w.appName = _appName)
		limit 1);
	
end
$$ language plpgsql;