CREATE OR REPLACE FUNCTION public.set_worker
  (_id integer, 
  _name text, 
  _app text, 
  _appfullname text, 
  _host text, 
  _date timestamp without time zone, 
  _active boolean, 
  _settings text, 
  _executed timestamp without time zone, 
  _error text, 
  _started timestamp without time zone,
  _statusTimeout int)
 RETURNS table
  (id integer, 
  name character varying, 
  host character varying,
  app character varying, 
  appfullname character varying, 
  date timestamp without time zone,
  settings text, active boolean, 
  executed timestamp without time zone, 
  hosts integer, 
  error text,
  activehosts integer, 
  started timestamp without time zone)
 LANGUAGE plpgsql
AS $function$
declare 
  _hosts int = 0;
  _activeHosts int = 0;
begin 
 
  _statusTimeout := coalesce(_statusTimeout, 60000);

  select count(1), sum(iif(w.active, 1, 0))
    into _hosts, _activeHosts
    from workers w
    where w.name = _name 
      and w.app = _app
      and w.date >= now() - (_statusTimeout * interval'1 millisecond')
      and w.host != _host;

  _hosts := _hosts + 1;
  _activeHosts := coalesce(_activeHosts, 0);
  _date := coalesce(_date, current_timestamp);

  update workers w
    set active = _active, 
      date = _date,
      hosts = _hosts,
      executed = _executed,
      error = _error,
      appFullName = _appFullName,
      started = _started
    where (_id > 0 and w.id = _id) or (w.host = _host and w.name = _name and w.app = _app)
    returning w.id
    into _id;
  
  if (_id is null or _id = 0)
    then 
    insert into workers
      (name, host, app, appFullName, date, settings, active, hosts, error, executed, started)
    values 
      (_name, _host, _app, _appFullName, _date, _settings, _active, _hosts, _error, _executed, _started); 
  end if;
    
 return query
 (select w.id, w.name, w.host, w.app, w.appFullName, w.date,
 w.settings, w.active, w.executed, w.hosts, w.error, _activeHosts as activeHosts, w.started
    from workers w
    where (_id > 0 and w.id = _id) or (w.host = _host and w.name = _name and w.app = _app)
    limit 1);
  
end
$function$
;